using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Web.Models;

namespace RestaurantManagement.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class StaffController : Controller
{
    private readonly IStaffService _staffService;

    public StaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    public async Task<IActionResult> Index(string tab = "attendance")
    {
        var employees = (await _staffService.GetEmployeesAsync()).ToList();
        var attendances = (await _staffService.GetTodayAttendanceAsync()).ToList();

        int presentCount = attendances.Count(a => a.Status == "Present" || a.Status == "Late");
        int leaveCount = attendances.Count(a => a.Status == "Leave" || a.Status == "Half day");

        var model = new StaffViewModel
        {
            ActiveTab = tab.ToLower(),
            Employees = employees,
            Attendances = attendances,
            TotalStaffCount = employees.Count,
            PresentStaffCount = presentCount,
            OnLeaveCount = leaveCount
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CheckIn(int employeeId, string status = "Present")
    {
        try
        {
            await _staffService.RecordAttendanceAsync(employeeId, status);
            TempData["Success"] = "Attendance marked successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "attendance" });
    }

    [HttpPost]
    public async Task<IActionResult> CheckOut(int employeeId)
    {
        try
        {
            await _staffService.RecordAttendanceAsync(employeeId, "Checked Out");
            TempData["Success"] = "Clocked out successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "attendance" });
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmployee(EmployeeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Phone))
        {
            TempData["Error"] = "Employee name and valid 10-digit mobile number are required.";
            return RedirectToAction(nameof(Index), new { tab = "staff" });
        }

        try
        {
            await _staffService.CreateEmployeeAsync(dto);
            TempData["Success"] = $"Employee '{dto.Name}' registered successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "staff" });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEmployee(int id, EmployeeDto dto)
    {
        try
        {
            await _staffService.UpdateEmployeeAsync(id, dto);
            TempData["Success"] = $"Employee '{dto.Name}' updated successfully!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "staff" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        bool success = await _staffService.DeleteEmployeeAsync(id);
        if (success) TempData["Success"] = "Employee deleted successfully.";
        else TempData["Error"] = "Unable to delete employee.";

        return RedirectToAction(nameof(Index), new { tab = "staff" });
    }
}
