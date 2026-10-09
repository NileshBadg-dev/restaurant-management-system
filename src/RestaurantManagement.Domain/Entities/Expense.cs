namespace RestaurantManagement.Domain.Entities;

public class Expense : BaseEntity
{
    public string Category { get; set; } = string.Empty; // Electricity, Rent, Gas, Maintenance, Transport
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    public string RecordedBy { get; set; } = string.Empty;
}
