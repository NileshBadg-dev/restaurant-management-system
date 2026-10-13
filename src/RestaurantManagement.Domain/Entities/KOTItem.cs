using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Domain.Entities;

public class KOTItem : BaseEntity
{
    public int KOTId { get; set; }
    public KOT? KOT { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string SpecialInstructions { get; set; } = string.Empty;
    public KOTStatus Status { get; set; } = KOTStatus.New;
    public int? OrderItemId { get; set; }
    public string Course { get; set; } = "Main";
    public bool IsCancelled { get; set; } = false;
    public string Station { get; set; } = "Main";
}
