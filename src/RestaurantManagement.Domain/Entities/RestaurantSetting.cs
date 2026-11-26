using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Domain.Entities;

public class RestaurantSetting : BaseEntity
{
    public string RestaurantName { get; set; } = "Hotel Kumbharwada";
    public string? RestaurantNameMr { get; set; } = "हॉटेल कुंभारवाडा";
    public string Tagline { get; set; } = "Authentic Village Dining & Hospitality";
    public string? TaglineMr { get; set; } = "अस्सल गावरान जेवण आणि आतिथ्य";
    public string Address { get; set; } = "101 Heritage Courtyard, Kumbharwada Road, Pune - 411002";
    public string? AddressMr { get; set; } = "१०१ हेरिटेज कोर्टयार्ड, कुंभारवाडा रोड, पुणे - ४११००२";
    public string AddressLine1 { get; set; } = "101 Heritage Courtyard, Kumbharwada Road";
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = "Pune";
    public string State { get; set; } = "Maharashtra";
    public string PinCode { get; set; } = "411002";
    public string Phone { get; set; } = "+91 98765 43210";
    public string Email { get; set; } = "contact@hotelkumbharwada.com";
    public string GSTIN { get; set; } = "27AAACH1234F1Z9";
    public string CurrencySymbol { get; set; } = "₹";
    public decimal CGSTPercentage { get; set; } = 2.5m;
    public decimal SGSTPercentage { get; set; } = 2.5m;
    public decimal ServiceChargePercentage { get; set; } = 0.0m;
    public string LogoUrl { get; set; } = "/assets/hotel-logo.jpg";
    public string InvoicePrefix { get; set; } = "INV";

    // Billing & Order Policies (non-mode operational policies)
    public int AutoMarkServedMinutes { get; set; } = 0; // Off=0, or 5 to 120 minutes
    public VoidApprovalSetting VoidApproval { get; set; } = VoidApprovalSetting.ManagerPIN;
    public BillingUnservedItemsSetting AllowBillingWithUnservedItems { get; set; } = BillingUnservedItemsSetting.Ask;
    public int DraftExpiryHours { get; set; } = 4;
    public decimal PackagingCharge { get; set; } = 15.0m;
    public decimal DeliveryCharge { get; set; } = 30.0m;
    public string ManagerPIN { get; set; } = "1234";
}

