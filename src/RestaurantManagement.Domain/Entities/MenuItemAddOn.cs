namespace RestaurantManagement.Domain.Entities;

public class MenuItemAddOn
{
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public int AddOnId { get; set; }
    public AddOn? AddOn { get; set; }
}
