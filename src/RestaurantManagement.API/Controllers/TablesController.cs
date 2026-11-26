using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TablesController : ControllerBase
{
    private readonly ITableService _tableService;

    public TablesController(ITableService tableService)
    {
        _tableService = tableService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tables = await _tableService.GetAllTablesAsync();
        return Ok(tables);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var table = await _tableService.GetTableByIdAsync(id);
        if (table == null) return NotFound();
        return Ok(table);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TableCreateUpdateDto dto)
    {
        var table = await _tableService.CreateTableAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = table.Id }, table);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TableCreateUpdateDto dto)
    {
        var table = await _tableService.UpdateTableAsync(id, dto);
        return Ok(table);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _tableService.DeleteTableAsync(id);
        if (!result) return NotFound();
        return Ok(new { Message = "Table deleted" });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] TableStatus status)
    {
        var result = await _tableService.UpdateTableStatusAsync(id, status);
        if (!result) return NotFound();
        return Ok(new { Message = "Status updated" });
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TableTransferDto dto)
    {
        var result = await _tableService.TransferTableAsync(dto.SourceTableId, dto.TargetTableId);
        if (!result) return BadRequest(new { Message = "Transfer failed. Target table may be occupied." });
        return Ok(new { Message = "Table transferred successfully" });
    }

    [HttpPost("merge")]
    public async Task<IActionResult> Merge([FromBody] TableMergeDto dto)
    {
        var result = await _tableService.MergeTablesAsync(dto.SourceTableId, dto.TargetTableId);
        if (!result) return BadRequest(new { Message = "Merge failed." });
        return Ok(new { Message = "Tables merged successfully" });
    }
}
