using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Domain.Entities;

public class Table : BaseEntity
{
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; } = 4;
    public int FloorId { get; set; }
    public Floor? Floor { get; set; }
    public TableStatus Status { get; set; } = TableStatus.Available;
    public int? CurrentOrderId { get; set; }
    public Order? CurrentOrder { get; set; }
    public int? WaiterId { get; set; }
    public User? Waiter { get; set; }
}
