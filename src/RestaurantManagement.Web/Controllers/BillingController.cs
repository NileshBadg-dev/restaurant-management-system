using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Web.Services;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = "Admin,Manager,Cashier")]
public class BillingController : Controller
{
    private readonly IBillingService _billingService;
    private readonly IOrderService _orderService;
    private readonly IWebConfigCacheService _configCache;

    public BillingController(IBillingService billingService, IOrderService orderService, IWebConfigCacheService configCache)
    {
        _billingService = billingService;
        _orderService = orderService;
        _configCache = configCache;
    }

    public async Task<IActionResult> Index()
    {
        var bills = await _billingService.GetRecentBillsAsync();
        return View(bills);
    }

    public async Task<IActionResult> Generate(int orderId)
    {
        if (orderId <= 0)
        {
            var activeOrders = await _orderService.GetActiveOrdersAsync();
            var firstOrder = activeOrders.FirstOrDefault();
            if (firstOrder == null)
            {
                TempData["Error"] = "No active orders found to generate bill.";
                return RedirectToAction(nameof(Index));
            }
            orderId = firstOrder.Id;
        }

        var bill = await _billingService.GenerateBillAsync(new CreateBillDto { OrderId = orderId });
        return RedirectToAction(nameof(Invoice), new { id = bill.Id });
    }

    public async Task<IActionResult> Invoice(int id)
    {
        var bill = await _billingService.GetBillByIdAsync(id);
        if (bill == null) return NotFound();
        ViewBag.Setting = await _configCache.GetSettingsAsync();
        return View(bill);
    }

    [HttpPost]
    public async Task<IActionResult> CollectPayment(CollectPaymentDto dto)
    {
        try
        {
            await _billingService.CollectPaymentAsync(dto);
            TempData["Success"] = "Payment collected successfully! Table status updated to Available.";
            return RedirectToAction(nameof(Invoice), new { id = dto.BillId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Invoice), new { id = dto.BillId });
        }
    }
}
