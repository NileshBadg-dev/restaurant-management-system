using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Application.DTOs;

public class InventoryItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = "KG";
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ReorderLevel { get; set; }
    public bool IsLowStock => CurrentStock <= MinimumStock;
}

public class StockAdjustmentDto
{
    public int InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public string TransactionType { get; set; } = "IN"; // IN, OUT, ADJUSTMENT, WASTAGE
    public string Notes { get; set; } = string.Empty;
}

public class RecipeDto
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<RecipeItemDto> Items { get; set; } = new();
}

public class RecipeItemDto
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public string InventoryItemName { get; set; } = string.Empty;
    public decimal QuantityRequired { get; set; }
    public string Unit { get; set; } = "KG";
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string RoleName => Role.ToString();
    public DateTime JoiningDate { get; set; }
    public decimal BaseSalary { get; set; }
    public string Shift { get; set; } = "Morning";
    public bool IsActive { get; set; }
}

public class AttendanceDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = "Present";
}

public class ExpenseDto
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string RecordedBy { get; set; } = string.Empty;
}

public class DashboardSummaryDto
{
    public decimal TodaySales { get; set; }
    public decimal YesterdaySales { get; set; }
    public int TodayOrdersCount { get; set; }
    public int TotalTablesCount { get; set; }
    public int OccupiedTablesCount { get; set; }
    public int AvailableTablesCount { get; set; }
    public int ReservedTablesCount { get; set; }
    public int PendingKOTCount { get; set; }
    public int CompletedOrdersCount { get; set; }
    public int CancelledOrdersCount { get; set; }
    public List<LowStockDto> LowStockAlerts { get; set; } = new();
    public List<TopSellingItemDto> TopSellingItems { get; set; } = new();
    public List<FloorDto> FloorTables { get; set; } = new();
}

public class LowStockDto
{
    public string ItemName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public string Unit { get; set; } = "KG";
    public string AlertLevel { get; set; } = "Low Stock"; // Low Stock or Critical
}

public class TopSellingItemDto
{
    public string MenuItemName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class SalesReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal DineInSales { get; set; }
    public decimal TakeawaySales { get; set; }
    public decimal DeliverySales { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalCGST { get; set; }
    public decimal TotalSGST { get; set; }
    public List<DailySalesSummaryDto> DailySummaries { get; set; } = new();
}

public class DailySalesSummaryDto
{
    public DateTime Date { get; set; }
    public decimal SalesAmount { get; set; }
    public int OrdersCount { get; set; }
}
