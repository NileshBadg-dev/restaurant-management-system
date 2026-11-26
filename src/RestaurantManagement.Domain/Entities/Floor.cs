using System.Text.Json.Serialization;

namespace RestaurantManagement.Domain.Entities;

public class Floor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("name_mr")]
    public string? NameMr { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 1;
    public ICollection<Table> Tables { get; set; } = new List<Table>();
}
