using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = "Admin,Manager,Waiter,Cashier")]
public class TablesController : Controller
{
    private readonly ITableService _tableService;
    private readonly IOrderService _orderService;

    public TablesController(ITableService tableService, IOrderService orderService)
    {
        _tableService = tableService;
        _orderService = orderService;
    }

    public async Task<IActionResult> Index()
    {
        var floors = await _tableService.GetFloorsWithTablesAsync();
        return View(floors);
    }

    public async Task<IActionResult> Details(int id)
    {
        var table = await _tableService.GetTableByIdAsync(id);
        if (table == null) return NotFound();

        OrderDto? currentOrder = null;
        if (table.CurrentOrderId.HasValue)
        {
            currentOrder = await _orderService.GetOrderByIdAsync(table.CurrentOrderId.Value);
        }

        ViewBag.CurrentOrder = currentOrder;
        ViewBag.AllTables = await _tableService.GetAllTablesAsync();
        return View(table);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int tableId, TableStatus status)
    {
        await _tableService.UpdateTableStatusAsync(tableId, status);
        return RedirectToAction(nameof(Details), new { id = tableId });
    }

    [HttpPost]
    public async Task<IActionResult> Transfer(int sourceTableId, int targetTableId)
    {
        bool success = await _tableService.TransferTableAsync(sourceTableId, targetTableId);
        if (!success) TempData["Error"] = "Transfer failed. Target table must be Available.";
        else TempData["Success"] = "Table transferred successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Merge(int sourceTableId, int targetTableId)
    {
        bool success = await _tableService.MergeTablesAsync(sourceTableId, targetTableId);
        if (!success) TempData["Error"] = "Merge failed.";
        else TempData["Success"] = "Tables merged successfully.";
        return RedirectToAction(nameof(Index));
    }
}
