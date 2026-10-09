using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Domain.Entities;

public class Reservation : BaseEntity
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime ReservationDate { get; set; }
    public int GuestCount { get; set; } = 2;
    public int? TableId { get; set; }
    public Table? Table { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string SpecialRequests { get; set; } = string.Empty;
}
