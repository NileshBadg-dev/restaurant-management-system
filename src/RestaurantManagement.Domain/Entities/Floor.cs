namespace RestaurantManagement.Domain.Entities;

public class Floor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 1;
    public ICollection<Table> Tables { get; set; } = new List<Table>();
}
