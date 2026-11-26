using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;

namespace RestaurantManagement.API.Controllers;

[ApiController]
[Route("api/menu-items")]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuItemsController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? categoryId, [FromQuery] bool? isVeg, [FromQuery] string? search)
    {
        var items = await _menuService.GetMenuItemsAsync(categoryId, isVeg, search);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _menuService.GetMenuItemByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MenuItemCreateUpdateDto dto)
    {
        var item = await _menuService.CreateMenuItemAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] MenuItemCreateUpdateDto dto)
    {
        var item = await _menuService.UpdateMenuItemAsync(id, dto);
        return Ok(item);
    }

    [HttpPut("{id}/toggle-availability")]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        var result = await _menuService.ToggleItemAvailabilityAsync(id);
        if (!result) return NotFound();
        return Ok(new { Message = "Availability updated" });
    }
}
