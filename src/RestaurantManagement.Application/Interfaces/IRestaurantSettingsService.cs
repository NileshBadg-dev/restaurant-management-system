using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Domain.Entities;

namespace RestaurantManagement.Application.Interfaces;

public interface IRestaurantSettingsService
{
    Task<RestaurantSetting> GetSettingsAsync();
    Task<PublicRestaurantSettingDto> GetPublicSettingsAsync();
    Task<RestaurantSetting> UpdateRestaurantConfigAsync(RestaurantConfigUpdateDto dto);
    Task<RestaurantSetting> UpdateFullSettingsAsync(RestaurantSetting updated);
}
