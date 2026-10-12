using RestaurantManagement.Application.DTOs;

namespace RestaurantManagement.Web.Models;

public class StaffViewModel
{
    public string ActiveTab { get; set; } = "attendance";
    public IEnumerable<EmployeeDto> Employees { get; set; } = new List<EmployeeDto>();
    public IEnumerable<AttendanceDto> Attendances { get; set; } = new List<AttendanceDto>();
    public int TotalStaffCount { get; set; }
    public int PresentStaffCount { get; set; }
    public int OnLeaveCount { get; set; }
}
