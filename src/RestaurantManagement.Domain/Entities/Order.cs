using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public OrderType OrderType { get; set; } = OrderType.DineIn;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    public int? TableId { get; set; }
    public Table? Table { get; set; }
    
    public int? WaiterId { get; set; }
    public User? Waiter { get; set; }
    
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string SpecialInstructions { get; set; } = string.Empty;
    
    public decimal SubTotal { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal TotalTax { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrandTotal { get; set; }
    
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<KOT> KOTs { get; set; } = new List<KOT>();
    public Bill? Bill { get; set; }
}
