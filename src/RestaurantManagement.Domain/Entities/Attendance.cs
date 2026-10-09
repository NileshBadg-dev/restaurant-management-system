namespace RestaurantManagement.Domain.Entities;

public class Attendance : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = "Present"; // Present, Absent, HalfDay, Leave
}
