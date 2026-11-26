using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Entities;

namespace RestaurantManagement.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<InventoryItemDto>> GetInventoryItemsAsync()
    {
        var items = await _unitOfWork.InventoryItems.GetAllAsync();
        return items.Where(i => !i.IsDeleted).Select(MapToDto);
    }

    public async Task<IEnumerable<InventoryItemDto>> GetLowStockItemsAsync()
    {
        var items = await _unitOfWork.InventoryItems.GetAllAsync();
        return items.Where(i => !i.IsDeleted && i.CurrentStock <= i.MinimumStock).Select(MapToDto);
    }

    public async Task<InventoryItemDto> AdjustStockAsync(StockAdjustmentDto dto)
    {
        var item = await _unitOfWork.InventoryItems.GetByIdAsync(dto.InventoryItemId);
        if (item == null) throw new KeyNotFoundException($"Inventory item {dto.InventoryItemId} not found");

        if (dto.TransactionType.ToUpper() == "IN")
        {
            item.CurrentStock += dto.Quantity;
        }
        else if (dto.TransactionType.ToUpper() == "OUT" || dto.TransactionType.ToUpper() == "WASTAGE")
        {
            if (item.CurrentStock < dto.Quantity) throw new InvalidOperationException($"Cannot deduct {dto.Quantity} {item.Unit}. Current stock is {item.CurrentStock} {item.Unit}.");
            item.CurrentStock -= dto.Quantity;
        }

        item.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.InventoryItems.UpdateAsync(item);

        var txn = new StockTransaction
        {
            InventoryItemId = item.Id,
            Quantity = dto.Quantity,
            TransactionType = dto.TransactionType,
            Notes = dto.Notes
        };
        await _unitOfWork.StockTransactions.AddAsync(txn);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task DeductStockForOrderAsync(int orderId)
    {
        var orderItems = await _unitOfWork.OrderItems.FindAsync(i => i.OrderId == orderId && !i.IsDeleted);
        var recipes = await _unitOfWork.Recipes.GetAllAsync();
        var recipeItems = await _unitOfWork.RecipeItems.GetAllAsync();

        foreach (var orderItem in orderItems)
        {
            var recipe = recipes.FirstOrDefault(r => r.MenuItemId == orderItem.MenuItemId && !r.IsDeleted);
            if (recipe != null)
            {
                var rItems = recipeItems.Where(ri => ri.RecipeId == recipe.Id && !ri.IsDeleted);
                foreach (var ri in rItems)
                {
                    decimal needed = ri.QuantityRequired * orderItem.Quantity;
                    var invItem = await _unitOfWork.InventoryItems.GetByIdAsync(ri.InventoryItemId);
                    if (invItem != null)
                    {
                        invItem.CurrentStock = Math.Max(0, invItem.CurrentStock - needed);
                        await _unitOfWork.InventoryItems.UpdateAsync(invItem);

                        await _unitOfWork.StockTransactions.AddAsync(new StockTransaction
                        {
                            InventoryItemId = invItem.Id,
                            Quantity = needed,
                            TransactionType = "RECIPE_DEDUCTION",
                            Notes = $"Auto-deducted for Order #{orderId}, Item: {orderItem.ItemName}"
                        });
                    }
                }
            }
        }
        await _unitOfWork.SaveChangesAsync();
    }

    private static InventoryItemDto MapToDto(InventoryItem i)
    {
        return new InventoryItemDto
        {
            Id = i.Id,
            Name = i.Name,
            Category = i.Category,
            Unit = i.Unit,
            CurrentStock = i.CurrentStock,
            MinimumStock = i.MinimumStock,
            UnitCost = i.UnitCost,
            ReorderLevel = i.ReorderLevel
        };
    }
}
