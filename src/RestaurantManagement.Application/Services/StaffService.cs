using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Entities;

namespace RestaurantManagement.Application.Services;

public class StaffService : IStaffService
{
    private readonly IUnitOfWork _unitOfWork;

    public StaffService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync()
    {
        var employees = await _unitOfWork.Employees.GetAllAsync();
        return employees.Where(e => !e.IsDeleted).Select(MapToDto);
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        var emp = await _unitOfWork.Employees.GetByIdAsync(id);
        if (emp == null || emp.IsDeleted) return null;
        return MapToDto(emp);
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(EmployeeDto dto)
    {
        var emp = new Employee
        {
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Role = dto.Role,
            JoiningDate = dto.JoiningDate == default ? DateTime.UtcNow : dto.JoiningDate,
            BaseSalary = dto.BaseSalary,
            Shift = string.IsNullOrWhiteSpace(dto.Shift) ? "Morning" : dto.Shift,
            IsActive = true
        };

        await _unitOfWork.Employees.AddAsync(emp);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(emp);
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(int id, EmployeeDto dto)
    {
        var emp = await _unitOfWork.Employees.GetByIdAsync(id);
        if (emp == null || emp.IsDeleted) throw new KeyNotFoundException($"Employee with id {id} not found.");

        emp.Name = dto.Name;
        emp.Email = dto.Email;
        emp.Phone = dto.Phone;
        emp.Role = dto.Role;
        emp.BaseSalary = dto.BaseSalary;
        emp.Shift = string.IsNullOrWhiteSpace(dto.Shift) ? emp.Shift : dto.Shift;
        emp.IsActive = dto.IsActive;
        emp.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Employees.UpdateAsync(emp);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(emp);
    }

    public async Task<bool> DeleteEmployeeAsync(int id)
    {
        var emp = await _unitOfWork.Employees.GetByIdAsync(id);
        if (emp == null || emp.IsDeleted) return false;

        emp.IsDeleted = true;
        emp.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Employees.UpdateAsync(emp);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AttendanceDto>> GetTodayAttendanceAsync()
    {
        var today = DateTime.UtcNow.Date;
        var attendances = await _unitOfWork.Attendances.FindAsync(a => a.Date == today && !a.IsDeleted);
        var employees = (await _unitOfWork.Employees.GetAllAsync()).ToDictionary(e => e.Id);

        return attendances.Select(a => new AttendanceDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = employees.TryGetValue(a.EmployeeId, out var e) ? e.Name : "",
            Date = a.Date,
            CheckInTime = a.CheckInTime,
            CheckOutTime = a.CheckOutTime,
            Status = a.Status
        });
    }

    public async Task<AttendanceDto> RecordAttendanceAsync(int employeeId, string status)
    {
        var today = DateTime.UtcNow.Date;
        var existing = (await _unitOfWork.Attendances.FindAsync(a => a.EmployeeId == employeeId && a.Date == today && !a.IsDeleted)).FirstOrDefault();

        if (existing == null)
        {
            existing = new Attendance
            {
                EmployeeId = employeeId,
                Date = today,
                CheckInTime = DateTime.UtcNow,
                Status = status
            };
            await _unitOfWork.Attendances.AddAsync(existing);
        }
        else
        {
            existing.CheckOutTime = DateTime.UtcNow;
            existing.Status = status;
            await _unitOfWork.Attendances.UpdateAsync(existing);
        }

        await _unitOfWork.SaveChangesAsync();

        var emp = await _unitOfWork.Employees.GetByIdAsync(employeeId);
        return new AttendanceDto
        {
            Id = existing.Id,
            EmployeeId = existing.EmployeeId,
            EmployeeName = emp?.Name ?? "",
            Date = existing.Date,
            CheckInTime = existing.CheckInTime,
            CheckOutTime = existing.CheckOutTime,
            Status = existing.Status
        };
    }

    public async Task<ExpenseDto> AddExpenseAsync(ExpenseDto dto)
    {
        var exp = new Expense
        {
            Category = dto.Category,
            Description = dto.Description,
            Amount = dto.Amount,
            ExpenseDate = DateTime.UtcNow,
            RecordedBy = dto.RecordedBy
        };

        await _unitOfWork.Expenses.AddAsync(exp);
        await _unitOfWork.SaveChangesAsync();

        dto.Id = exp.Id;
        dto.ExpenseDate = exp.ExpenseDate;
        return dto;
    }

    public async Task<IEnumerable<ExpenseDto>> GetExpensesAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var expenses = await _unitOfWork.Expenses.GetAllAsync();
        var query = expenses.Where(e => !e.IsDeleted);

        if (startDate.HasValue) query = query.Where(e => e.ExpenseDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(e => e.ExpenseDate <= endDate.Value);

        return query.OrderByDescending(e => e.ExpenseDate).Select(e => new ExpenseDto
        {
            Id = e.Id,
            Category = e.Category,
            Description = e.Description,
            Amount = e.Amount,
            ExpenseDate = e.ExpenseDate,
            RecordedBy = e.RecordedBy
        });
    }

    private static EmployeeDto MapToDto(Employee e)
    {
        return new EmployeeDto
        {
            Id = e.Id,
            Name = e.Name,
            Email = e.Email,
            Phone = e.Phone,
            Role = e.Role,
            JoiningDate = e.JoiningDate,
            BaseSalary = e.BaseSalary,
            Shift = e.Shift,
            IsActive = e.IsActive
        };
    }
}
