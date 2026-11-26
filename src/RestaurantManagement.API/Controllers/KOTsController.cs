using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KOTsController : ControllerBase
{
    private readonly IKOTService _kotService;

    public KOTsController(IKOTService kotService)
    {
        _kotService = kotService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveKOTs()
    {
        var kots = await _kotService.GetActiveKOTsAsync();
        return Ok(kots);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var kot = await _kotService.GetKOTByIdAsync(id);
        if (kot == null) return NotFound();
        return Ok(kot);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] KOTStatus status)
    {
        var kot = await _kotService.UpdateKOTStatusAsync(id, status);
        return Ok(kot);
    }
}
