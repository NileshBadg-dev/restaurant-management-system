using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Application.Services;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Web.Models;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class MastersController : Controller
{
    private static readonly HashSet<string> AllowedLogoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".svg", ".webp"
    };

    private static readonly HashSet<string> AllowedLogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/jpg", "image/svg+xml", "image/webp"
    };

    private const long MaxLogoSizeBytes = 1024 * 1024; // 1 MB

    private readonly ITableService _tableService;
    private readonly IMenuService _menuService;
    private readonly IStaffService _staffService;
    private readonly IInventoryService _inventoryService;
    private readonly IRestaurantSettingsService _settingsService;
    private readonly IWebConfigCacheService _configCache;
    private readonly IWebHostEnvironment _env;

    public MastersController(
        ITableService tableService, 
        IMenuService menuService, 
        IStaffService staffService,
        IInventoryService inventoryService,
        IRestaurantSettingsService settingsService,
        IWebConfigCacheService configCache,
        IWebHostEnvironment env)
    {
        _tableService = tableService;
        _menuService = menuService;
        _staffService = staffService;
        _inventoryService = inventoryService;
        _settingsService = settingsService;
        _configCache = configCache;
        _env = env;
    }

    public async Task<IActionResult> Index(string tab = "floor", string? returnUrl = null, bool openAdd = false)
    {
        var setting = await _configCache.GetSettingsAsync();

        var model = new MastersViewModel
        {
            ActiveTab = tab.ToLower(),
            ReturnUrl = returnUrl,
            OpenAdd = openAdd,
            Floors = await _tableService.GetFloorsWithTablesAsync(),
            Tables = await _tableService.GetAllTablesAsync(),
            Categories = await _menuService.GetCategoriesAsync(),
            MenuItems = await _menuService.GetMenuItemsAsync(),
            Employees = await _staffService.GetEmployeesAsync(),
            InventoryItems = await _inventoryService.GetInventoryItemsAsync(),
            Setting = setting
        };

        return View("Index", model);
    }

    [HttpGet]
    public Task<IActionResult> Settings()
    {
        return Index(tab: "settings");
    }

    // ================= Floor Operations =================
    [HttpPost]
    public async Task<IActionResult> CreateFloor(FloorCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            TempData["Error"] = "Floor name is required.";
            return RedirectToAction(nameof(Index), new { tab = "floor" });
        }

        await _tableService.CreateFloorAsync(dto);
        TempData["Success"] = $"Floor '{dto.Name}' created successfully.";
        return RedirectToAction(nameof(Index), new { tab = "floor" });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateFloor(int id, FloorCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            TempData["Error"] = "Floor name is required.";
            return RedirectToAction(nameof(Index), new { tab = "floor" });
        }

        await _tableService.UpdateFloorAsync(id, dto);
        TempData["Success"] = $"Floor '{dto.Name}' updated successfully.";
        return RedirectToAction(nameof(Index), new { tab = "floor" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteFloor(int id)
    {
        bool success = await _tableService.DeleteFloorAsync(id);
        if (success) TempData["Success"] = "Floor deleted successfully.";
        else TempData["Error"] = "Could not delete floor.";
        return RedirectToAction(nameof(Index), new { tab = "floor" });
    }

    // ================= Table Operations =================
    [HttpPost]
    public async Task<IActionResult> CreateTable(TableCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TableNumber))
        {
            TempData["Error"] = "Table number is required.";
            return RedirectToAction(nameof(Index), new { tab = "table" });
        }

        await _tableService.CreateTableAsync(dto);
        TempData["Success"] = $"Table '{dto.TableNumber}' created successfully.";
        return RedirectToAction(nameof(Index), new { tab = "table" });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTable(int id, TableCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TableNumber))
        {
            TempData["Error"] = "Table number is required.";
            return RedirectToAction(nameof(Index), new { tab = "table" });
        }

        await _tableService.UpdateTableAsync(id, dto);
        TempData["Success"] = $"Table '{dto.TableNumber}' updated successfully.";
        return RedirectToAction(nameof(Index), new { tab = "table" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTable(int id)
    {
        bool success = await _tableService.DeleteTableAsync(id);
        if (success) TempData["Success"] = "Table deleted successfully.";
        else TempData["Error"] = "Could not delete table.";
        return RedirectToAction(nameof(Index), new { tab = "table" });
    }

    // ================= Menu Item Operations =================
    [HttpPost]
    public async Task<IActionResult> CreateMenuItem(MenuItemCreateUpdateDto dto, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
        {
            TempData["Error"] = "Valid Item Name and positive price are required.";
            return string.IsNullOrEmpty(returnUrl) 
                ? RedirectToAction(nameof(Index), new { tab = "menu" }) 
                : Redirect(returnUrl);
        }

        await _menuService.CreateMenuItemAsync(dto);
        TempData["Success"] = $"Menu item '{dto.Name}' added successfully.";

        if (!string.IsNullOrEmpty(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index), new { tab = "menu" });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateMenuItem(int id, MenuItemCreateUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
        {
            TempData["Error"] = "Valid Item Name and positive price are required.";
            return RedirectToAction(nameof(Index), new { tab = "menu" });
        }

        await _menuService.UpdateMenuItemAsync(id, dto);
        TempData["Success"] = $"Menu item '{dto.Name}' updated successfully.";
        return RedirectToAction(nameof(Index), new { tab = "menu" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMenuItem(int id)
    {
        bool success = await _menuService.DeleteMenuItemAsync(id);
        if (success) TempData["Success"] = "Menu item deleted successfully.";
        else TempData["Error"] = "Could not delete menu item.";
        return RedirectToAction(nameof(Index), new { tab = "menu" });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleMenuItemAvailability(int id, string? tab = "menu")
    {
        await _menuService.ToggleItemAvailabilityAsync(id);
        return RedirectToAction(nameof(Index), new { tab = tab ?? "menu" });
    }

    // ================= Category Operations =================
    [HttpPost]
    public async Task<IActionResult> CreateCategory(CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            TempData["Error"] = "Category name is required.";
            return RedirectToAction(nameof(Index), new { tab = "category" });
        }

        await _menuService.CreateCategoryAsync(dto);
        TempData["Success"] = $"Category '{dto.Name}' created successfully.";
        return RedirectToAction(nameof(Index), new { tab = "category" });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCategory(int id, CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            TempData["Error"] = "Category name is required.";
            return RedirectToAction(nameof(Index), new { tab = "category" });
        }

        await _menuService.UpdateCategoryAsync(id, dto);
        TempData["Success"] = $"Category '{dto.Name}' updated successfully.";
        return RedirectToAction(nameof(Index), new { tab = "category" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        bool success = await _menuService.DeleteCategoryAsync(id);
        if (success) TempData["Success"] = "Category deleted successfully.";
        else TempData["Error"] = "Could not delete category.";
        return RedirectToAction(nameof(Index), new { tab = "category" });
    }

    // ================= Employee Operations =================
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(EmployeeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Phone))
        {
            TempData["Error"] = "Employee Name and Phone number are required.";
            return RedirectToAction(nameof(Index), new { tab = "employee" });
        }

        await _staffService.CreateEmployeeAsync(dto);
        TempData["Success"] = $"Employee '{dto.Name}' ({dto.Role}) registered successfully.";
        return RedirectToAction(nameof(Index), new { tab = "employee" });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEmployee(int id, EmployeeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Phone))
        {
            TempData["Error"] = "Employee Name and Phone number are required.";
            return RedirectToAction(nameof(Index), new { tab = "employee" });
        }

        await _staffService.UpdateEmployeeAsync(id, dto);
        TempData["Success"] = $"Employee '{dto.Name}' updated successfully.";
        return RedirectToAction(nameof(Index), new { tab = "employee" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        bool success = await _staffService.DeleteEmployeeAsync(id);
        if (success) TempData["Success"] = "Employee removed successfully.";
        else TempData["Error"] = "Could not remove employee.";
        return RedirectToAction(nameof(Index), new { tab = "employee" });
    }

    // ================= Restaurant Settings Operations =================
    [HttpPost]
    public async Task<IActionResult> UpdateRestaurantConfig([FromForm] RestaurantConfigUpdateDto dto, IFormFile? logoFile)
    {
        if (string.IsNullOrWhiteSpace(dto.RestaurantName))
        {
            return BadRequest(new { success = false, messageKey = "validation.restaurantNameRequired", message = "Restaurant name is required." });
        }

        if (!string.IsNullOrWhiteSpace(dto.PinCode) && !System.Text.RegularExpressions.Regex.IsMatch(dto.PinCode.Trim(), @"^[A-Za-z0-9\-\s]{4,10}$"))
        {
            return BadRequest(new { success = false, messageKey = "validation.pinInvalid", message = "Please enter a valid PIN code (4 to 10 digits)." });
        }

        var currentSetting = await _settingsService.GetSettingsAsync();
        string? newLogoUrl = null;

        if (logoFile != null && logoFile.Length > 0)
        {
            if (logoFile.Length > MaxLogoSizeBytes)
            {
                return BadRequest(new { success = false, messageKey = "validation.logoTooLarge", message = "Logo file size must not exceed 1 MB." });
            }

            var ext = Path.GetExtension(logoFile.FileName);
            if (string.IsNullOrEmpty(ext) || !AllowedLogoExtensions.Contains(ext) ||
                (!string.IsNullOrEmpty(logoFile.ContentType) && !AllowedLogoContentTypes.Contains(logoFile.ContentType)))
            {
                return BadRequest(new { success = false, messageKey = "validation.logoInvalidType", message = "Logo must be a PNG, JPG, SVG, or WebP image." });
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"logo_{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            DeleteUploadedLogoIfLocal(currentSetting.LogoUrl);
            newLogoUrl = $"/uploads/{fileName}";
            dto.RemoveLogo = false;
        }
        else if (dto.RemoveLogo)
        {
            DeleteUploadedLogoIfLocal(currentSetting.LogoUrl);
        }

        dto.LogoUrl = newLogoUrl;
        var saved = await _settingsService.UpdateRestaurantConfigAsync(dto);
        _configCache.InvalidateSettings();

        var publicDto = RestaurantSettingsService.MapToPublicDto(saved);
        return Json(new
        {
            success = true,
            messageKey = "toast.settingsSaved",
            message = "Restaurant settings saved successfully.",
            settings = publicDto
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSettings(RestaurantSetting updated)
    {
        if (string.IsNullOrWhiteSpace(updated.RestaurantName))
        {
            TempData["Error"] = "Restaurant name is required.";
            return RedirectToAction(nameof(Index), new { tab = "settings" });
        }

        await _settingsService.UpdateFullSettingsAsync(updated);
        _configCache.InvalidateSettings();
        TempData["Success"] = "Restaurant settings & tax configuration saved successfully.";
        return RedirectToAction(nameof(Index), new { tab = "settings" });
    }

    private void DeleteUploadedLogoIfLocal(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl) || !logoUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relative = logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_env.WebRootPath, relative);
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }

    // ================= Inventory Stock Adjustment =================
    [HttpPost]
    public async Task<IActionResult> AdjustStock(StockAdjustmentDto dto)
    {
        try
        {
            await _inventoryService.AdjustStockAsync(dto);
            TempData["Success"] = "Stock adjustment recorded successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { tab = "inventory" });
    }
}
