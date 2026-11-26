using Microsoft.Extensions.Caching.Memory;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Application.Services;
using RestaurantManagement.Domain.Entities;

namespace RestaurantManagement.Web.Services;

public class WebConfigCacheService : IWebConfigCacheService
{
    private const string SettingsCacheKey = "RMS_RestaurantSetting_Full";
    private const string PublicSettingsCacheKey = "RMS_RestaurantSetting_Public";

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _env;

    public WebConfigCacheService(
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _env = env;
    }

    public async Task<RestaurantSetting> GetSettingsAsync()
    {
        if (_cache.TryGetValue(SettingsCacheKey, out RestaurantSetting? cached) && cached != null)
        {
            return cached;
        }

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRestaurantSettingsService>();
        var setting = await service.GetSettingsAsync();

        _cache.Set(SettingsCacheKey, setting, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30)
        });
        return setting;
    }

    public async Task<PublicRestaurantSettingDto> GetPublicSettingsAsync()
    {
        if (_cache.TryGetValue(PublicSettingsCacheKey, out PublicRestaurantSettingDto? cached) && cached != null)
        {
            return cached;
        }

        var setting = await GetSettingsAsync();
        var dto = RestaurantSettingsService.MapToPublicDto(setting);

        _cache.Set(PublicSettingsCacheKey, dto, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30)
        });
        return dto;
    }

    public void InvalidateSettings()
    {
        _cache.Remove(SettingsCacheKey);
        _cache.Remove(PublicSettingsCacheKey);
    }

    public string GetI18nDictionaryJson(string lang)
    {
        var normalized = lang == "mr" ? "mr" : "en";
        var cacheKey = $"RMS_I18n_Dict_{normalized}";
        if (_cache.TryGetValue(cacheKey, out string? cachedJson) && !string.IsNullOrEmpty(cachedJson))
        {
            return cachedJson;
        }

        var filePath = Path.Combine(_env.WebRootPath, "i18n", $"{normalized}.json");
        var json = File.Exists(filePath) ? File.ReadAllText(filePath) : "{}";

        _cache.Set(cacheKey, json, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30)
        });
        return json;
    }
}
