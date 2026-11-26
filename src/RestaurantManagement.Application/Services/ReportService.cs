using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate)
    {
        var orders = await _unitOfWork.Orders.GetAllAsync();
        var filtered = orders.Where(o => !o.IsDeleted && o.Status == OrderStatus.Completed && o.CreatedAt >= startDate && o.CreatedAt <= endDate).ToList();

        decimal totalRevenue = filtered.Sum(o => o.GrandTotal);
        decimal dineInSales = filtered.Where(o => o.OrderType == OrderType.DineIn).Sum(o => o.GrandTotal);
        decimal takeawaySales = filtered.Where(o => o.OrderType == OrderType.Takeaway).Sum(o => o.GrandTotal);
        decimal deliverySales = filtered.Where(o => o.OrderType == OrderType.Delivery).Sum(o => o.GrandTotal);

        var dailySummaries = filtered.GroupBy(o => o.CreatedAt.Date)
            .Select(g => new DailySalesSummaryDto
            {
                Date = g.Key,
                SalesAmount = g.Sum(o => o.GrandTotal),
                OrdersCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new SalesReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRevenue = totalRevenue,
            TotalOrders = filtered.Count,
            DineInSales = dineInSales,
            TakeawaySales = takeawaySales,
            DeliverySales = deliverySales,
            TotalDiscount = filtered.Sum(o => o.DiscountAmount),
            TotalCGST = filtered.Sum(o => o.CGST),
            TotalSGST = filtered.Sum(o => o.SGST),
            DailySummaries = dailySummaries
        };
    }
}
