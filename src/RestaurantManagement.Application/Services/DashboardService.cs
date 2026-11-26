using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITableService _tableService;

    public DashboardService(IUnitOfWork unitOfWork, ITableService tableService)
    {
        _unitOfWork = unitOfWork;
        _tableService = tableService;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var orders = await _unitOfWork.Orders.GetAllAsync();
        var activeOrders = orders.Where(o => !o.IsDeleted).ToList();

        var todayOrders = activeOrders.Where(o => o.CreatedAt.Date == today && o.Status == OrderStatus.Completed).ToList();
        var yesterdayOrders = activeOrders.Where(o => o.CreatedAt.Date == yesterday && o.Status == OrderStatus.Completed).ToList();

        decimal todaySales = todayOrders.Sum(o => o.GrandTotal);
        decimal yesterdaySales = yesterdayOrders.Sum(o => o.GrandTotal);

        var tables = (await _unitOfWork.Tables.GetAllAsync()).Where(t => !t.IsDeleted).ToList();
        int totalTables = tables.Count;
        int occupiedTables = tables.Count(t => t.Status == TableStatus.Occupied || t.Status == TableStatus.Billing);
        int availableTables = tables.Count(t => t.Status == TableStatus.Available);
        int reservedTables = tables.Count(t => t.Status == TableStatus.Reserved);

        var kots = await _unitOfWork.KOTs.GetAllAsync();
        int pendingKots = kots.Count(k => !k.IsDeleted && (k.Status == KOTStatus.New || k.Status == KOTStatus.Preparing));

        int completedCount = activeOrders.Count(o => o.Status == OrderStatus.Completed);
        int cancelledCount = activeOrders.Count(o => o.Status == OrderStatus.Cancelled);
        int newOrdersCount = activeOrders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed);
        int preparingOrdersCount = activeOrders.Count(o => o.Status == OrderStatus.Preparing);
        int readyOrdersCount = activeOrders.Count(o => o.Status == OrderStatus.Ready);
        int servedOrdersCount = activeOrders.Count(o => o.Status == OrderStatus.Completed);

        var inventory = await _unitOfWork.InventoryItems.GetAllAsync();
        var lowStock = inventory.Where(i => !i.IsDeleted && i.CurrentStock <= i.MinimumStock)
            .Select(i => new LowStockDto
            {
                ItemName = i.Name,
                CurrentStock = i.CurrentStock,
                MinimumStock = i.MinimumStock,
                Unit = i.Unit,
                AlertLevel = i.CurrentStock <= (i.MinimumStock / 2m) ? "Critical" : "Low Stock"
            }).ToList();

        var orderItems = await _unitOfWork.OrderItems.GetAllAsync();
        var menuItemsList = (await _unitOfWork.MenuItems.GetAllAsync()).Where(m => !m.IsDeleted).ToList();
        var menuMrById = menuItemsList.ToDictionary(m => m.Id, m => m.NameMr);
        var topSelling = orderItems.Where(i => !i.IsDeleted)
            .GroupBy(i => new { i.MenuItemId, i.ItemName })
            .Select(g => new TopSellingItemDto
            {
                MenuItemName = g.Key.ItemName,
                MenuItemNameMr = menuMrById.TryGetValue(g.Key.MenuItemId, out var mr) ? mr : null,
                CategoryName = "Main Menu",
                TotalQuantitySold = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(t => t.TotalQuantitySold)
            .Take(5)
            .ToList();

        var floorTables = (await _tableService.GetFloorsWithTablesAsync()).ToList();

        var sevenDaysAgo = today.AddDays(-6);
        var weeklyOrders = activeOrders.Where(o => o.CreatedAt.Date >= sevenDaysAgo && o.CreatedAt.Date <= today).ToList();
        var weeklyTrend = new List<DailySalesSummaryDto>();
        for (int i = 6; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            var dayOrders = weeklyOrders.Where(o => o.CreatedAt.Date == d && o.Status == OrderStatus.Completed).ToList();
            weeklyTrend.Add(new DailySalesSummaryDto
            {
                Date = d,
                SalesAmount = dayOrders.Sum(o => o.GrandTotal),
                OrdersCount = dayOrders.Count
            });
        }

        int dineInCount = activeOrders.Count(o => o.OrderType == OrderType.DineIn && o.CreatedAt.Date == today);
        int takeawayCount = activeOrders.Count(o => o.OrderType == OrderType.Takeaway && o.CreatedAt.Date == today);
        int deliveryCount = activeOrders.Count(o => o.OrderType == OrderType.Delivery && o.CreatedAt.Date == today);
        if (dineInCount == 0 && takeawayCount == 0 && deliveryCount == 0 && activeOrders.Any())
        {
            dineInCount = activeOrders.Count(o => o.OrderType == OrderType.DineIn);
            takeawayCount = activeOrders.Count(o => o.OrderType == OrderType.Takeaway);
            deliveryCount = activeOrders.Count(o => o.OrderType == OrderType.Delivery);
        }

        int totalOrdersCalculated = dineInCount + takeawayCount + deliveryCount;

        var allEmployees = (await _unitOfWork.Employees.GetAllAsync()).Where(e => !e.IsDeleted).ToList();
        var todayAttendances = (await _unitOfWork.Attendances.FindAsync(a => a.Date == today && !a.IsDeleted && a.Status == "Present")).ToList();

        var roleAttendanceList = new List<RoleStaffCountDto>();
        var rolesToReport = new[] { UserRole.Manager, UserRole.Cook, UserRole.Waiter, UserRole.Housekeeping, UserRole.Cashier, UserRole.KitchenStaff, UserRole.Admin };

        foreach (var role in rolesToReport)
        {
            var roleEmployees = allEmployees.Where(e => e.Role == role).ToList();
            if (roleEmployees.Any())
            {
                int totalRole = roleEmployees.Count;
                int presentRole = roleEmployees.Count(e => todayAttendances.Any(a => a.EmployeeId == e.Id) || e.IsActive);
                string displayRoleName = role switch
                {
                    UserRole.Cook => "Chefs / Cooks",
                    UserRole.KitchenStaff => "Kitchen Staff",
                    UserRole.Housekeeping => "Housekeeping",
                    UserRole.Waiter => "Waiters",
                    UserRole.Manager => "Managers",
                    UserRole.Cashier => "Cashiers",
                    UserRole.Admin => "Admins",
                    _ => role.ToString()
                };

                roleAttendanceList.Add(new RoleStaffCountDto
                {
                    RoleName = displayRoleName,
                    TotalCount = totalRole,
                    PresentCount = presentRole
                });
            }
        }

        int totalStaff = allEmployees.Count;
        int presentStaff = allEmployees.Count(e => todayAttendances.Any(a => a.EmployeeId == e.Id) || e.IsActive);

        var tableDict = tables.ToDictionary(t => t.Id, t => t.TableNumber.Replace("Table", "").Trim());
        var orderItemQtyDict = orderItems.Where(i => !i.IsDeleted)
            .GroupBy(i => i.OrderId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var recentOrders = activeOrders
            .OrderByDescending(o => o.CreatedAt)
            .Take(6)
            .Select(o => new RecentOrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderType = o.OrderType == OrderType.DineIn ? "Dine-in" : o.OrderType.ToString(),
                Status = o.Status.ToString(),
                TableId = o.TableId,
                TableNumber = o.TableId.HasValue && tableDict.TryGetValue(o.TableId.Value, out var num) ? num : string.Empty,
                GrandTotal = o.GrandTotal,
                TotalItems = orderItemQtyDict.TryGetValue(o.Id, out var qty) ? qty : 0,
                CreatedAt = o.CreatedAt
            })
            .ToList();

        return new DashboardSummaryDto
        {
            TodaySales = todaySales,
            YesterdaySales = yesterdaySales,
            TodayOrdersCount = totalOrdersCalculated > 0 ? totalOrdersCalculated : (activeOrders.Count(o => o.CreatedAt.Date == today)),
            TotalTablesCount = totalTables,
            OccupiedTablesCount = occupiedTables,
            AvailableTablesCount = availableTables,
            ReservedTablesCount = reservedTables,
            PendingKOTCount = pendingKots,
            CompletedOrdersCount = completedCount,
            CancelledOrdersCount = cancelledCount,
            NewOrdersCount = newOrdersCount,
            PreparingOrdersCount = preparingOrdersCount,
            ReadyOrdersCount = readyOrdersCount,
            ServedOrdersCount = servedOrdersCount,
            LowStockAlerts = lowStock,
            TopSellingItems = topSelling,
            FloorTables = floorTables,
            WeeklySalesTrend = weeklyTrend,
            DineInOrdersCount = dineInCount,
            TakeawayOrdersCount = takeawayCount,
            DeliveryOrdersCount = deliveryCount,
            StaffRoleAttendance = roleAttendanceList,
            TotalStaffCount = totalStaff,
            PresentStaffCount = presentStaff,
            RecentOrders = recentOrders
        };
    }
}
