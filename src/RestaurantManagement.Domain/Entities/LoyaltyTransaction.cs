namespace RestaurantManagement.Domain.Entities;

public class LoyaltyTransaction : BaseEntity
{
    public int LoyaltyAccountId { get; set; }
    public LoyaltyAccount? LoyaltyAccount { get; set; }
    public int Points { get; set; }
    public string TransactionType { get; set; } = "EARNED"; // EARNED or REDEEMED
    public string ReferenceOrderNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
