namespace RestaurantManagement.Application.DTOs;

public class PublicRestaurantSettingDto
{
    public string RestaurantName { get; set; } = "Hotel Kumbharwada";
    public string? RestaurantNameMr { get; set; } = "हॉटेल कुंभारवाडा";
    public string Tagline { get; set; } = "Authentic Village Dining & Hospitality";
    public string? TaglineMr { get; set; } = "अस्सल गावरान जेवण आणि आतिथ्य";
    public string LogoUrl { get; set; } = "/assets/hotel-logo.jpg";
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PinCode { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string? AddressMr { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
}

public class RestaurantConfigUpdateDto
{
    public string RestaurantName { get; set; } = string.Empty;
    public string? RestaurantNameMr { get; set; }
    public string? Tagline { get; set; }
    public string? TaglineMr { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PinCode { get; set; }
    public string? AddressMr { get; set; }
    public string? Phone { get; set; }
    public string? LogoUrl { get; set; }
    public bool RemoveLogo { get; set; }
}
