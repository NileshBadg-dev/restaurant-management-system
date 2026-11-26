using RestaurantManagement.Application.DTOs;

namespace RestaurantManagement.Application.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
    Task<CustomerDto?> GetCustomerByIdAsync(int id);
    Task<CustomerDto?> GetCustomerByPhoneAsync(string phone);
    Task<CustomerDto> CreateOrUpdateCustomerAsync(CustomerCreateUpdateDto dto);
    Task<int> AwardLoyaltyPointsAsync(int customerId, decimal orderAmount, string orderNo);
}
