namespace RestaurantManagement.Domain.Entities;

public class OrderItemAddOn : BaseEntity
{
    public int OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }
    
    public int AddOnId { get; set; }
    public AddOn? AddOn { get; set; }
    
    public string AddOnName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
