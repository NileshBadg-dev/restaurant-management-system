using RestaurantManagement.Application.DTOs;

namespace RestaurantManagement.Application.Interfaces;

public interface IMenuService
{
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<CategoryDto> CreateCategoryAsync(CategoryDto dto);
    Task<CategoryDto> UpdateCategoryAsync(int id, CategoryDto dto);
    Task<bool> DeleteCategoryAsync(int id);
    Task<IEnumerable<MenuItemDto>> GetMenuItemsAsync(int? categoryId = null, bool? isVeg = null, string? search = null);
    Task<MenuItemDto?> GetMenuItemByIdAsync(int id);
    Task<MenuItemDto> CreateMenuItemAsync(MenuItemCreateUpdateDto dto);
    Task<MenuItemDto> UpdateMenuItemAsync(int id, MenuItemCreateUpdateDto dto);
    Task<bool> ToggleItemAvailabilityAsync(int id);
    Task<bool> DeleteMenuItemAsync(int id);
}
