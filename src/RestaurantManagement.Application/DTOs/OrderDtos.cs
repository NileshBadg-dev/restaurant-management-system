using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Application.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public string OrderTypeName => OrderType.ToString();
    public OrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    
    public int? TableId { get; set; }
    public string? TableNumber { get; set; }
    
    public int? WaiterId { get; set; }
    public string? WaiterName { get; set; }
    
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string SpecialInstructions { get; set; } = string.Empty;
    
    public decimal SubTotal { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal TotalTax { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public List<OrderItemDto> Items { get; set; } = new();
    public List<KOTDto> KOTs { get; set; } = new();
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string SpecialInstructions { get; set; } = string.Empty;
    public string SelectedVariantName { get; set; } = string.Empty;
    public List<OrderItemAddOnDto> AddOns { get; set; } = new();
}

public class OrderItemAddOnDto
{
    public int Id { get; set; }
    public int AddOnId { get; set; }
    public string AddOnName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class CreateOrderDto
{
    public OrderType OrderType { get; set; } = OrderType.DineIn;
    public int? TableId { get; set; }
    public int? WaiterId { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string SpecialInstructions { get; set; } = string.Empty;
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public string SpecialInstructions { get; set; } = string.Empty;
    public string SelectedVariantName { get; set; } = string.Empty;
    public List<int> SelectedAddOnIds { get; set; } = new();
}

public class KOTDto
{
    public int Id { get; set; }
    public string KOTNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string WaiterName { get; set; } = string.Empty;
    public KOTStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<KOTItemDto> Items { get; set; } = new();
}

public class KOTItemDto
{
    public int Id { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string SpecialInstructions { get; set; } = string.Empty;
    public KOTStatus Status { get; set; }
    public string StatusName => Status.ToString();
}

public class UpdateKOTStatusDto
{
    public int KOTId { get; set; }
    public KOTStatus Status { get; set; }
}
