namespace RestaurantManagement.Domain.Entities;

public class Recipe : BaseEntity
{
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public ICollection<RecipeItem> RecipeItems { get; set; } = new List<RecipeItem>();
}
