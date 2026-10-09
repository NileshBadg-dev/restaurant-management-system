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
    
    public ICollection<KOTItem> Items { get; set; } = new List<KOTItem>();
}
