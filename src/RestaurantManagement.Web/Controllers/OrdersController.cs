using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Web.Controllers;

public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IMenuService _menuService;
    private readonly ITableService _tableService;
    private readonly IStaffService _staffService;

    public OrdersController(
        IOrderService orderService, 
        IMenuService menuService, 
        ITableService tableService,
        IStaffService staffService)
    {
        _orderService = orderService;
        _menuService = menuService;
        _tableService = tableService;
        _staffService = staffService;
    }

    public async Task<IActionResult> Index(OrderStatus? status, OrderType? type)
    {
        var orders = await _orderService.GetOrdersAsync(status, type);
        return View(orders);
    }

    public async Task<IActionResult> POS(int? tableId, string? returnUrl = null)
    {
        ViewBag.Categories = await _menuService.GetCategoriesAsync();
        ViewBag.MenuItems = await _menuService.GetMenuItemsAsync();
        ViewBag.Tables = await _tableService.GetAllTablesAsync();
        ViewBag.SelectedTableId = tableId;
        ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Dashboard" : returnUrl;

        var employees = await _staffService.GetEmployeesAsync();
        ViewBag.Waiters = employees.Where(e => e.Role == UserRole.Waiter || e.Role == UserRole.Manager || e.Role == UserRole.Cashier).ToList();

        TableDto? selectedTable = null;
        if (tableId.HasValue)
        {
            selectedTable = await _tableService.GetTableByIdAsync(tableId.Value);
        }
        ViewBag.SelectedTable = selectedTable;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreatePOSOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var order = await _orderService.CreateOrderAsync(dto);
            // Auto generate KOT on POS submit
            await _orderService.GenerateKOTAsync(order.Id, dto.SpecialInstructions);
            return Json(new { success = true, orderId = order.Id, orderNumber = order.OrderNumber });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateKOT(int orderId, string notes = "")
    {
        await _orderService.GenerateKOTAsync(orderId, notes);
        TempData["Success"] = "KOT generated and sent to Kitchen Display System!";
        return RedirectToAction("Index", "Kitchen");
    }
}
