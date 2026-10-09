using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Application.Interfaces;

public interface IKOTService
{
    Task<IEnumerable<KOTDto>> GetActiveKOTsAsync();
    Task<KOTDto?> GetKOTByIdAsync(int id);
    Task<KOTDto> UpdateKOTStatusAsync(int kotId, KOTStatus status);
}

public interface IBillingService
{
    Task<BillDto> GenerateBillAsync(CreateBillDto dto);
    Task<BillDto?> GetBillByIdAsync(int id);
    Task<BillDto?> GetBillByOrderIdAsync(int orderId);
    Task<BillDto> CollectPaymentAsync(CollectPaymentDto dto);
    Task<IEnumerable<BillDto>> GetRecentBillsAsync();
}
