using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Entities;

namespace RestaurantManagement.Application.Services;

public class RestaurantSettingsService : IRestaurantSettingsService
{
    private readonly IUnitOfWork _unitOfWork;

    public RestaurantSettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RestaurantSetting> GetSettingsAsync()
    {
        var settings = await _unitOfWork.RestaurantSettings.GetAllAsync();
        return settings.FirstOrDefault() ?? new RestaurantSetting();
    }

    public async Task<PublicRestaurantSettingDto> GetPublicSettingsAsync()
    {
        var setting = await GetSettingsAsync();
        return MapToPublicDto(setting);
    }

    public async Task<RestaurantSetting> UpdateRestaurantConfigAsync(RestaurantConfigUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RestaurantName))
        {
            throw new ArgumentException("Restaurant name is required.");
        }

        var settings = await _unitOfWork.RestaurantSettings.GetAllAsync();
        var setting = settings.FirstOrDefault();
        bool isNew = setting == null;
        setting ??= new RestaurantSetting();

        setting.RestaurantName = dto.RestaurantName.Trim();
        setting.RestaurantNameMr = string.IsNullOrWhiteSpace(dto.RestaurantNameMr) ? null : dto.RestaurantNameMr.Trim();
        setting.Tagline = (dto.Tagline ?? string.Empty).Trim();
        setting.TaglineMr = string.IsNullOrWhiteSpace(dto.TaglineMr) ? null : dto.TaglineMr.Trim();
        setting.AddressLine1 = (dto.AddressLine1 ?? string.Empty).Trim();
        setting.AddressLine2 = (dto.AddressLine2 ?? string.Empty).Trim();
        setting.City = (dto.City ?? string.Empty).Trim();
        setting.State = (dto.State ?? string.Empty).Trim();
        setting.PinCode = (dto.PinCode ?? string.Empty).Trim();
        setting.AddressMr = string.IsNullOrWhiteSpace(dto.AddressMr) ? null : dto.AddressMr.Trim();
        setting.Phone = (dto.Phone ?? string.Empty).Trim();
        setting.Address = BuildFormattedAddress(setting);

        if (dto.RemoveLogo)
        {
            setting.LogoUrl = string.Empty;
        }
        else if (dto.LogoUrl != null)
        {
            setting.LogoUrl = dto.LogoUrl;
        }

        setting.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            await _unitOfWork.RestaurantSettings.AddAsync(setting);
        }
        else
        {
            await _unitOfWork.RestaurantSettings.UpdateAsync(setting);
        }

        await _unitOfWork.SaveChangesAsync();
        return setting;
    }

    public async Task<RestaurantSetting> UpdateFullSettingsAsync(RestaurantSetting updated)
    {
        if (string.IsNullOrWhiteSpace(updated.RestaurantName))
        {
            throw new ArgumentException("Restaurant name is required.");
        }

        var settings = await _unitOfWork.RestaurantSettings.GetAllAsync();
        var setting = settings.FirstOrDefault();
        if (setting == null)
        {
            updated.Address = string.IsNullOrWhiteSpace(updated.Address) ? BuildFormattedAddress(updated) : updated.Address;
            await _unitOfWork.RestaurantSettings.AddAsync(updated);
            setting = updated;
        }
        else
        {
            setting.RestaurantName = updated.RestaurantName.Trim();
            if (!string.IsNullOrWhiteSpace(updated.RestaurantNameMr))
            {
                setting.RestaurantNameMr = updated.RestaurantNameMr.Trim();
            }
            setting.Tagline = (updated.Tagline ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(updated.TaglineMr))
            {
                setting.TaglineMr = updated.TaglineMr.Trim();
            }
            setting.AddressLine1 = (updated.AddressLine1 ?? string.Empty).Trim();
            setting.AddressLine2 = (updated.AddressLine2 ?? string.Empty).Trim();
            setting.City = (updated.City ?? string.Empty).Trim();
            setting.State = (updated.State ?? string.Empty).Trim();
            setting.PinCode = (updated.PinCode ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(updated.AddressMr))
            {
                setting.AddressMr = updated.AddressMr.Trim();
            }
            setting.Address = !string.IsNullOrWhiteSpace(setting.AddressLine1) || !string.IsNullOrWhiteSpace(setting.City)
                ? BuildFormattedAddress(setting)
                : updated.Address;
            setting.Phone = (updated.Phone ?? string.Empty).Trim();
            setting.Email = (updated.Email ?? string.Empty).Trim();
            setting.GSTIN = (updated.GSTIN ?? string.Empty).Trim();
            setting.CurrencySymbol = string.IsNullOrWhiteSpace(updated.CurrencySymbol) ? "₹" : updated.CurrencySymbol.Trim();
            setting.CGSTPercentage = updated.CGSTPercentage;
            setting.SGSTPercentage = updated.SGSTPercentage;
            setting.ServiceChargePercentage = updated.ServiceChargePercentage;
            setting.AutoMarkServedMinutes = updated.AutoMarkServedMinutes;
            setting.VoidApproval = updated.VoidApproval;
            setting.AllowBillingWithUnservedItems = updated.AllowBillingWithUnservedItems;
            setting.DraftExpiryHours = updated.DraftExpiryHours;
            setting.PackagingCharge = updated.PackagingCharge;
            setting.DeliveryCharge = updated.DeliveryCharge;
            if (!string.IsNullOrWhiteSpace(updated.ManagerPIN))
            {
                setting.ManagerPIN = updated.ManagerPIN.Trim();
            }
            setting.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.RestaurantSettings.UpdateAsync(setting);
        }

        await _unitOfWork.SaveChangesAsync();
        return setting;
    }

    public static PublicRestaurantSettingDto MapToPublicDto(RestaurantSetting setting)
    {
        var fullAddress = BuildFormattedAddress(setting);
        if (string.IsNullOrWhiteSpace(fullAddress))
        {
            fullAddress = setting.Address;
        }

        return new PublicRestaurantSettingDto
        {
            RestaurantName = setting.RestaurantName,
            RestaurantNameMr = setting.RestaurantNameMr,
            Tagline = setting.Tagline,
            TaglineMr = setting.TaglineMr,
            LogoUrl = setting.LogoUrl ?? string.Empty,
            AddressLine1 = setting.AddressLine1 ?? string.Empty,
            AddressLine2 = setting.AddressLine2 ?? string.Empty,
            City = setting.City ?? string.Empty,
            State = setting.State ?? string.Empty,
            PinCode = setting.PinCode ?? string.Empty,
            FullAddress = fullAddress,
            AddressMr = setting.AddressMr,
            Phone = setting.Phone ?? string.Empty,
            Email = setting.Email ?? string.Empty,
            GSTIN = setting.GSTIN ?? string.Empty
        };
    }

    public static string BuildFormattedAddress(RestaurantSetting setting)
    {
        var parts = new List<string>(5);
        if (!string.IsNullOrWhiteSpace(setting.AddressLine1)) parts.Add(setting.AddressLine1.Trim());
        if (!string.IsNullOrWhiteSpace(setting.AddressLine2)) parts.Add(setting.AddressLine2.Trim());
        if (!string.IsNullOrWhiteSpace(setting.City)) parts.Add(setting.City.Trim());
        if (!string.IsNullOrWhiteSpace(setting.State)) parts.Add(setting.State.Trim());

        var address = string.Join(", ", parts);
        if (!string.IsNullOrWhiteSpace(setting.PinCode))
        {
            address = string.IsNullOrWhiteSpace(address)
                ? setting.PinCode.Trim()
                : $"{address} - {setting.PinCode.Trim()}";
        }
        return address;
    }
}
