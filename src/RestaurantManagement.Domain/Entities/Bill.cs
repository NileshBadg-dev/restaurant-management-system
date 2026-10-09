namespace RestaurantManagement.Domain.Entities;

public class Bill : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public DateTime BillDate { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal GrandTotal { get; set; }
    public bool IsPaid { get; set; } = false;
    
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
