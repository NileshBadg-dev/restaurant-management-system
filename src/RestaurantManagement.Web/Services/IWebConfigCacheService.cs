using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Domain.Entities;

namespace RestaurantManagement.Web.Services;

public interface IWebConfigCacheService
{
    Task<RestaurantSetting> GetSettingsAsync();
    Task<PublicRestaurantSettingDto> GetPublicSettingsAsync();
    void InvalidateSettings();
    string GetI18nDictionaryJson(string lang);
}
