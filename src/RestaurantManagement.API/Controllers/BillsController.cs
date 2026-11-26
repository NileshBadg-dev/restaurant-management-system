using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;

namespace RestaurantManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillsController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillsController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentBills()
    {
        var bills = await _billingService.GetRecentBillsAsync();
        return Ok(bills);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var bill = await _billingService.GetBillByIdAsync(id);
        if (bill == null) return NotFound();
        return Ok(bill);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateBill([FromBody] CreateBillDto dto)
    {
        var bill = await _billingService.GenerateBillAsync(dto);
        return Ok(bill);
    }

    [HttpPost("payment")]
    public async Task<IActionResult> CollectPayment([FromBody] CollectPaymentDto dto)
    {
        var bill = await _billingService.CollectPaymentAsync(dto);
        return Ok(bill);
    }
}
