using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;

namespace RestaurantManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FloorsController : ControllerBase
{
    private readonly ITableService _tableService;

    public FloorsController(ITableService tableService)
    {
        _tableService = tableService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFloorsWithTables()
    {
        var floors = await _tableService.GetFloorsWithTablesAsync();
        return Ok(floors);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FloorCreateUpdateDto dto)
    {
        var floor = await _tableService.CreateFloorAsync(dto);
        return Ok(floor);
    }
}
