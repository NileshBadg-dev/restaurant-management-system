using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] OrderStatus? status, [FromQuery] OrderType? type)
    {
        var orders = await _orderService.GetOrdersAsync(status, type);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var order = await _orderService.CreateOrderAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPost("{id}/items")]
    public async Task<IActionResult> AddItem(int id, [FromBody] CreateOrderItemDto dto)
    {
        var order = await _orderService.AddItemToOrderAsync(id, dto);
        return Ok(order);
    }

    [HttpDelete("{id}/items/{orderItemId}")]
    public async Task<IActionResult> RemoveItem(int id, int orderItemId)
    {
        var order = await _orderService.RemoveItemFromOrderAsync(id, orderItemId);
        return Ok(order);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatus status)
    {
        var order = await _orderService.UpdateOrderStatusAsync(id, status);
        return Ok(order);
    }

    [HttpPost("{id}/kot")]
    public async Task<IActionResult> GenerateKOT(int id, [FromQuery] string notes = "")
    {
        var kot = await _orderService.GenerateKOTAsync(id, notes);
        return Ok(kot);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromQuery] string reason = "")
    {
        var result = await _orderService.CancelOrderAsync(id, reason);
        if (!result) return NotFound();
        return Ok(new { Message = "Order cancelled" });
    }
}
