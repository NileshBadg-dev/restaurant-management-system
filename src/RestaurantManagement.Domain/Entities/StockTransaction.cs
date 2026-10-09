namespace RestaurantManagement.Domain.Entities;

public class StockTransaction : BaseEntity
{
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public decimal Quantity { get; set; }
    public string TransactionType { get; set; } = "IN"; // IN, OUT, ADJUSTMENT, WASTAGE, RECIPE_DEDUCTION
    public string Notes { get; set; } = string.Empty;
}
