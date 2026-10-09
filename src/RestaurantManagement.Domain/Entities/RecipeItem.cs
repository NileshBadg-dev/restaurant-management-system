namespace RestaurantManagement.Domain.Entities;

public class RecipeItem : BaseEntity
{
    public int RecipeId { get; set; }
    public Recipe? Recipe { get; set; }
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public decimal QuantityRequired { get; set; }
    public string Unit { get; set; } = "KG";
}
