using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;

namespace RestaurantManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public StaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _staffService.GetEmployeesAsync();
        return Ok(employees);
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] EmployeeDto dto)
    {
        var employee = await _staffService.CreateEmployeeAsync(dto);
        return Ok(employee);
    }

    [HttpGet("expenses")]
    public async Task<IActionResult> GetExpenses([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var expenses = await _staffService.GetExpensesAsync(startDate, endDate);
        return Ok(expenses);
    }

    [HttpPost("expenses")]
    public async Task<IActionResult> AddExpense([FromBody] ExpenseDto dto)
    {
        var exp = await _staffService.AddExpenseAsync(dto);
        return Ok(exp);
    }
}
