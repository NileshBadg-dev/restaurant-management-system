using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Domain.Entities;

public class Payment : BaseEntity
{
    public int BillId { get; set; }
    public Bill? Bill { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public decimal Amount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
}
