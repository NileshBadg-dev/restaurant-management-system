using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Domain.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string SpecialInstructions { get; set; } = string.Empty;
    public string SelectedVariantName { get; set; } = string.Empty;

    // Order Management Specifications (Section 3 & 5)
    public OrderItemStatus Status { get; set; } = OrderItemStatus.Pending;
    public int? KOTId { get; set; }
    public string KOTNumber { get; set; } = string.Empty;
    public string Course { get; set; } = "Main"; // FR-OM-09 (Starter, Main, Dessert, Hold)
    public bool IsHeld { get; set; } = false; // OM-C10/C11
    public string Station { get; set; } = "Main"; // OM-A20 (Tandoor, Main, Bar)
    public DateTime? SentAt { get; set; }
    public DateTime? PreparingAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? ServedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string CancelReason { get; set; } = string.Empty;
    public bool IsWastage { get; set; } = false; // OM-B04, OM-B05
    public decimal WastageCost { get; set; } = 0m;
    public bool IsComplimentary { get; set; } = false; // OM-B19
    public string ComplimentaryReason { get; set; } = string.Empty;
    public bool IsBillAdjusted { get; set; } = false; // OM-B06
    
    public ICollection<OrderItemAddOn> OrderItemAddOns { get; set; } = new List<OrderItemAddOn>();
}

