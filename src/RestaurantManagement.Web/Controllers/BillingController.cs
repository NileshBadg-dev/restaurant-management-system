using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;

namespace RestaurantManagement.Web.Controllers;

public class BillingController : Controller
{
    private readonly IBillingService _billingService;
    private readonly IOrderService _orderService;

    public BillingController(IBillingService billingService, IOrderService orderService)
    {
        _billingService = billingService;
        _orderService = orderService;
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
