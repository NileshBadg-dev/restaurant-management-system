namespace RestaurantManagement.Domain.Entities;

public class MenuItemVariant : BaseEntity
{
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
