namespace RestaurantManagement.Domain.Entities;

public class RestaurantSetting : BaseEntity
{
    public string RestaurantName { get; set; } = "Girana Garden Fine Dine";
    public string Address { get; set; } = "101 Grand Galleria, MG Road, Ahmedabad";
    public string Phone { get; set; } = "+91 98765 43210";
    public string Email { get; set; } = "contact@giranagarden.com";
    public string GSTIN { get; set; } = "24AAACG1234F1Z9";
    public string CurrencySymbol { get; set; } = "₹";
    public decimal CGSTPercentage { get; set; } = 2.5m;
    public decimal SGSTPercentage { get; set; } = 2.5m;
    public decimal ServiceChargePercentage { get; set; } = 0.0m;
    public string LogoUrl { get; set; } = "/images/logo.png";
    public string InvoicePrefix { get; set; } = "INV";
}
