using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = "Admin,Manager,Waiter,Cashier")]
public class MenuController : Controller
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await _menuService.GetCategoriesAsync();
        var items = await _menuService.GetMenuItemsAsync();
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        await _menuService.ToggleItemAvailabilityAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Create(MenuItemCreateUpdateDto dto, string? returnUrl = null)
    {
        await _menuService.CreateMenuItemAsync(dto);
        TempData["Success"] = "New menu item added successfully!";
        if (!string.IsNullOrEmpty(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(Index));
    }
}
