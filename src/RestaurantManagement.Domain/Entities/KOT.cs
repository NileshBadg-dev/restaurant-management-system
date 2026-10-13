using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Domain.Entities;

public class KOT : BaseEntity
{
    public string KOTNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string WaiterName { get; set; } = string.Empty;
    public KOTStatus Status { get; set; } = KOTStatus.New;
    public string Notes { get; set; } = string.Empty;
    public KOTType Type { get; set; } = KOTType.Initial;
    public string Station { get; set; } = "All"; // OM-A20
    public bool IsRush { get; set; } = false; // OM-G02
    public bool IsPrinted { get; set; } = true;
    public bool PrintFailed { get; set; } = false; // OM-A19
    public bool AcknowledgedByChef { get; set; } = false; // OM-G07
    public bool InformedKitchenConfirmed { get; set; } = false; // FR-OM-05
    public string Reason { get; set; } = string.Empty;
    
    public ICollection<KOTItem> Items { get; set; } = new List<KOTItem>();
}
