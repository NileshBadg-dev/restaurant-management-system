using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Domain.Entities;

public class LoyaltyAccount : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int CurrentPoints { get; set; } = 0;
    public int TotalEarnedPoints { get; set; } = 0;
    public LoyaltyTier Tier { get; set; } = LoyaltyTier.Silver;
    
    public ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
}
