using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
    {
        // Business Rule: Table must be available or occupied for dine-in
        Table? table = null;
        if (dto.OrderType == OrderType.DineIn && dto.TableId.HasValue)
        {
            table = await _unitOfWork.Tables.GetByIdAsync(dto.TableId.Value);
            if (table == null || table.IsDeleted || table.Status == TableStatus.OutOfService)
            {
                throw new InvalidOperationException("Selected table is unavailable for new orders.");
            }

            // Customer adding items to existing table order
            if (table.CurrentOrderId.HasValue && (table.Status == TableStatus.Occupied || table.Status == TableStatus.Billing))
            {
                var existingOrder = await _unitOfWork.Orders.GetByIdAsync(table.CurrentOrderId.Value);
                if (existingOrder != null && existingOrder.Status != OrderStatus.Completed && existingOrder.Status != OrderStatus.Cancelled)
                {
                    decimal additionalSubtotal = 0m;
                    foreach (var itemDto in dto.Items)
                    {
                        if (itemDto.Quantity <= 0) continue;
                        var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(itemDto.MenuItemId);
                        if (menuItem == null || menuItem.IsDeleted || !menuItem.IsAvailable) continue;

                        decimal price = menuItem.Price;
                        decimal itemTotal = price * itemDto.Quantity;
                        additionalSubtotal += itemTotal;

                        existingOrder.Items.Add(new OrderItem
                        {
                            OrderId = existingOrder.Id,
                            MenuItemId = menuItem.Id,
                            ItemName = menuItem.Name,
                            UnitPrice = price,
                            Quantity = itemDto.Quantity,
                            TotalPrice = itemTotal,
                            SpecialInstructions = itemDto.SpecialInstructions
                        });
                    }

                    existingOrder.SubTotal += additionalSubtotal;
                    existingOrder.CGST = Math.Round(existingOrder.SubTotal * 0.025m, 2);
                    existingOrder.SGST = Math.Round(existingOrder.SubTotal * 0.025m, 2);
                    existingOrder.TotalTax = existingOrder.CGST + existingOrder.SGST;
                    existingOrder.GrandTotal = existingOrder.SubTotal + existingOrder.TotalTax - existingOrder.DiscountAmount;
                    if (dto.WaiterId.HasValue && dto.WaiterId.Value > 0) existingOrder.WaiterId = dto.WaiterId;
                    if (!string.IsNullOrWhiteSpace(dto.SpecialInstructions))
                    {
                        existingOrder.SpecialInstructions = string.IsNullOrWhiteSpace(existingOrder.SpecialInstructions)
                            ? dto.SpecialInstructions
                            : $"{existingOrder.SpecialInstructions}; {dto.SpecialInstructions}";
                    }

                    await _unitOfWork.Orders.UpdateAsync(existingOrder);
                    await _unitOfWork.SaveChangesAsync();

                    return await GetOrderByIdAsync(existingOrder.Id) ?? throw new InvalidOperationException("Order update failed.");
                }
            }
        }

        int count = await _unitOfWork.Orders.CountAsync();
        string orderNo = $"ORD-{(count + 1001)}";

        var order = new Order
        {
            OrderNumber = orderNo,
            OrderType = dto.OrderType,
            Status = OrderStatus.Pending,
            TableId = dto.TableId,
            WaiterId = dto.WaiterId,
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            SpecialInstructions = dto.SpecialInstructions,
            CreatedAt = DateTime.UtcNow
        };

        decimal subtotal = 0m;

        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");

            var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(itemDto.MenuItemId);
            if (menuItem == null || menuItem.IsDeleted || !menuItem.IsAvailable)
            {
                throw new InvalidOperationException($"Menu item '{menuItem?.Name ?? itemDto.MenuItemId.ToString()}' is currently sold out or unavailable.");
            }

            decimal price = menuItem.Price;
            string variantName = itemDto.SelectedVariantName;

            if (!string.IsNullOrEmpty(variantName))
            {
                var variant = (await _unitOfWork.MenuItemVariants.FindAsync(v => v.MenuItemId == menuItem.Id && v.VariantName == variantName)).FirstOrDefault();
                if (variant != null) price = variant.Price;
            }

            decimal itemTotal = price * itemDto.Quantity;

            // Add-ons calculation
            var orderItemAddOns = new List<OrderItemAddOn>();
            foreach (var addOnId in itemDto.SelectedAddOnIds)
            {
                var addOn = await _unitOfWork.AddOns.GetByIdAsync(addOnId);
                if (addOn != null && addOn.IsAvailable)
                {
                    itemTotal += addOn.Price * itemDto.Quantity;
                    orderItemAddOns.Add(new OrderItemAddOn
                    {
                        AddOnId = addOn.Id,
                        AddOnName = addOn.Name,
                        Price = addOn.Price
                    });
                }
            }

            subtotal += itemTotal;

            order.Items.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                ItemName = menuItem.Name,
                UnitPrice = price,
                Quantity = itemDto.Quantity,
                TotalPrice = itemTotal,
                SpecialInstructions = itemDto.SpecialInstructions,
                SelectedVariantName = variantName,
                OrderItemAddOns = orderItemAddOns
            });
        }

        // Standard Indian GST (5% total = CGST 2.5% + SGST 2.5%)
        order.SubTotal = subtotal;
        order.CGST = Math.Round(subtotal * 0.025m, 2);
        order.SGST = Math.Round(subtotal * 0.025m, 2);
        order.TotalTax = order.CGST + order.SGST;
        order.GrandTotal = order.SubTotal + order.TotalTax - order.DiscountAmount;

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        if (table != null)
        {
            table.Status = TableStatus.Occupied;
            table.CurrentOrderId = order.Id;
            table.WaiterId = dto.WaiterId;
            await _unitOfWork.Tables.UpdateAsync(table);
            await _unitOfWork.SaveChangesAsync();
        }

        return await GetOrderByIdAsync(order.Id) ?? throw new InvalidOperationException("Order creation failed.");
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(id);
        if (order == null || order.IsDeleted) return null;

        var items = await _unitOfWork.OrderItems.FindAsync(i => i.OrderId == id && !i.IsDeleted);
        var table = order.TableId.HasValue ? await _unitOfWork.Tables.GetByIdAsync(order.TableId.Value) : null;
        var waiter = order.WaiterId.HasValue ? await _unitOfWork.Users.GetByIdAsync(order.WaiterId.Value) : null;
        var kots = await _unitOfWork.KOTs.FindAsync(k => k.OrderId == id && !k.IsDeleted);

        var dto = MapToOrderDto(order, table?.TableNumber, waiter?.FullName);
        dto.Items = items.Select(MapToOrderItemDto).ToList();
        dto.KOTs = kots.Select(MapToKOTDto).ToList();
        return dto;
    }

    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber)
    {
        var orders = await _unitOfWork.Orders.FindAsync(o => o.OrderNumber == orderNumber && !o.IsDeleted);
        var order = orders.FirstOrDefault();
        return order != null ? await GetOrderByIdAsync(order.Id) : null;
    }

    public async Task<IEnumerable<OrderDto>> GetActiveOrdersAsync()
    {
        return await GetOrdersAsync(status: null);
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(OrderStatus? status = null, OrderType? type = null)
    {
        var orders = await _unitOfWork.Orders.GetAllAsync();
        var tables = (await _unitOfWork.Tables.GetAllAsync()).ToDictionary(t => t.Id);
        var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.Id);

        var query = orders.Where(o => !o.IsDeleted);

        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (type.HasValue) query = query.Where(o => o.OrderType == type.Value);

        return query.OrderByDescending(o => o.CreatedAt).Select(o => MapToOrderDto(o, o.TableId.HasValue && tables.TryGetValue(o.TableId.Value, out var t) ? t.TableNumber : null, o.WaiterId.HasValue && users.TryGetValue(o.WaiterId.Value, out var u) ? u.FullName : null));
    }

    public async Task<OrderDto> AddItemToOrderAsync(int orderId, CreateOrderItemDto dto)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot modify completed or cancelled order.");
        }

        var menuItem = await _unitOfWork.MenuItems.GetByIdAsync(dto.MenuItemId);
        if (menuItem == null || !menuItem.IsAvailable) throw new InvalidOperationException("Item is unavailable.");

        decimal price = menuItem.Price;
        if (!string.IsNullOrEmpty(dto.SelectedVariantName))
        {
            var variant = (await _unitOfWork.MenuItemVariants.FindAsync(v => v.MenuItemId == menuItem.Id && v.VariantName == dto.SelectedVariantName)).FirstOrDefault();
            if (variant != null) price = variant.Price;
        }

        decimal total = price * dto.Quantity;

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            MenuItemId = menuItem.Id,
            ItemName = menuItem.Name,
            UnitPrice = price,
            Quantity = dto.Quantity,
            TotalPrice = total,
            SpecialInstructions = dto.SpecialInstructions,
            SelectedVariantName = dto.SelectedVariantName
        };

        await _unitOfWork.OrderItems.AddAsync(orderItem);

        // Recalculate totals
        var allItems = (await _unitOfWork.OrderItems.FindAsync(i => i.OrderId == order.Id && !i.IsDeleted)).ToList();
        allItems.Add(orderItem);

        decimal subtotal = allItems.Sum(i => i.TotalPrice);
        order.SubTotal = subtotal;
        order.CGST = Math.Round(subtotal * 0.025m, 2);
        order.SGST = Math.Round(subtotal * 0.025m, 2);
        order.TotalTax = order.CGST + order.SGST;
        order.GrandTotal = order.SubTotal + order.TotalTax - order.DiscountAmount;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return await GetOrderByIdAsync(orderId) ?? throw new InvalidOperationException("Update failed.");
    }

    public async Task<OrderDto> RemoveItemFromOrderAsync(int orderId, int orderItemId)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot modify completed or cancelled order.");
        }

        var item = await _unitOfWork.OrderItems.GetByIdAsync(orderItemId);
        if (item != null)
        {
            item.IsDeleted = true;
            await _unitOfWork.OrderItems.UpdateAsync(item);

            var remainingItems = (await _unitOfWork.OrderItems.FindAsync(i => i.OrderId == order.Id && !i.IsDeleted && i.Id != orderItemId)).ToList();
            decimal subtotal = remainingItems.Sum(i => i.TotalPrice);
            order.SubTotal = subtotal;
            order.CGST = Math.Round(subtotal * 0.025m, 2);
            order.SGST = Math.Round(subtotal * 0.025m, 2);
            order.TotalTax = order.CGST + order.SGST;
            order.GrandTotal = order.SubTotal + order.TotalTax - order.DiscountAmount;
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
        }

        return await GetOrderByIdAsync(orderId) ?? throw new InvalidOperationException("Update failed.");
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(int orderId, OrderStatus status)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null) throw new KeyNotFoundException($"Order {orderId} not found");

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return await GetOrderByIdAsync(orderId) ?? throw new InvalidOperationException("Update failed.");
    }

    public async Task<KOTDto> GenerateKOTAsync(int orderId, string notes = "")
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null) throw new KeyNotFoundException($"Order {orderId} not found");

        var items = (await _unitOfWork.OrderItems.FindAsync(i => i.OrderId == orderId && !i.IsDeleted)).ToList();
        var table = order.TableId.HasValue ? await _unitOfWork.Tables.GetByIdAsync(order.TableId.Value) : null;
        var waiter = order.WaiterId.HasValue ? await _unitOfWork.Users.GetByIdAsync(order.WaiterId.Value) : null;

        int kotCount = await _unitOfWork.KOTs.CountAsync();
        string kotNo = $"KOT-{(kotCount + 1001)}";

        var kot = new KOT
        {
            KOTNumber = kotNo,
            OrderId = orderId,
            TableNumber = table?.TableNumber ?? "NA",
            WaiterName = waiter?.FullName ?? "Staff",
            Status = KOTStatus.New,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in items)
        {
            kot.Items.Add(new KOTItem
            {
                MenuItemName = item.ItemName + (!string.IsNullOrEmpty(item.SelectedVariantName) ? $" ({item.SelectedVariantName})" : ""),
                Quantity = item.Quantity,
                SpecialInstructions = item.SpecialInstructions,
                Status = KOTStatus.New
            });
        }

        await _unitOfWork.KOTs.AddAsync(kot);

        order.Status = OrderStatus.Confirmed;
        await _unitOfWork.Orders.UpdateAsync(order);

        await _unitOfWork.SaveChangesAsync();

        return MapToKOTDto(kot);
    }

    public async Task<bool> CancelOrderAsync(int orderId, string reason = "")
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
        if (order == null) return false;

        order.Status = OrderStatus.Cancelled;
        order.SpecialInstructions += $" [Cancelled: {reason}]";
        order.UpdatedAt = DateTime.UtcNow;

        if (order.TableId.HasValue)
        {
            var table = await _unitOfWork.Tables.GetByIdAsync(order.TableId.Value);
            if (table != null)
            {
                table.Status = TableStatus.Available;
                table.CurrentOrderId = null;
                await _unitOfWork.Tables.UpdateAsync(table);
            }
        }

        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static OrderDto MapToOrderDto(Order o, string? tableNumber, string? waiterName)
    {
        return new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            OrderType = o.OrderType,
            Status = o.Status,
            TableId = o.TableId,
            TableNumber = tableNumber,
            WaiterId = o.WaiterId,
            WaiterName = waiterName,
            CustomerId = o.CustomerId,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            SpecialInstructions = o.SpecialInstructions,
            SubTotal = o.SubTotal,
            CGST = o.CGST,
            SGST = o.SGST,
            TotalTax = o.TotalTax,
            DiscountAmount = o.DiscountAmount,
            GrandTotal = o.GrandTotal,
            CreatedAt = o.CreatedAt
        };
    }

    private static OrderItemDto MapToOrderItemDto(OrderItem i)
    {
        return new OrderItemDto
        {
            Id = i.Id,
            OrderId = i.OrderId,
            MenuItemId = i.MenuItemId,
            ItemName = i.ItemName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            TotalPrice = i.TotalPrice,
            SpecialInstructions = i.SpecialInstructions,
            SelectedVariantName = i.SelectedVariantName,
            AddOns = i.OrderItemAddOns?.Select(a => new OrderItemAddOnDto
            {
                Id = a.Id,
                AddOnId = a.AddOnId,
                AddOnName = a.AddOnName,
                Price = a.Price
            }).ToList() ?? new List<OrderItemAddOnDto>()
        };
    }

    private static KOTDto MapToKOTDto(KOT k)
    {
        return new KOTDto
        {
            Id = k.Id,
            KOTNumber = k.KOTNumber,
            OrderId = k.OrderId,
            TableNumber = k.TableNumber,
            WaiterName = k.WaiterName,
            Status = k.Status,
            Notes = k.Notes,
            CreatedAt = k.CreatedAt,
            Items = k.Items.Select(i => new KOTItemDto
            {
                Id = i.Id,
                MenuItemName = i.MenuItemName,
                Quantity = i.Quantity,
                SpecialInstructions = i.SpecialInstructions,
                Status = i.Status
            }).ToList()
        };
    }
}
