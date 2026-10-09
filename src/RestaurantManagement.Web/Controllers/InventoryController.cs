using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;

namespace RestaurantManagement.Web.Controllers;

public class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _inventoryService.GetInventoryItemsAsync();
        return View(items);
    }

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
        return RedirectToAction(nameof(Index));
    }
}
