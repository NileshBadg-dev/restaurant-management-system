using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto?> GetOrderByNumberAsync(string orderNumber);
    Task<IEnumerable<OrderDto>> GetActiveOrdersAsync();
    Task<IEnumerable<OrderDto>> GetOrdersAsync(OrderStatus? status = null, OrderType? type = null);
    Task<OrderDto> AddItemToOrderAsync(int orderId, CreateOrderItemDto dto);
    Task<OrderDto> RemoveItemFromOrderAsync(int orderId, int orderItemId);
    Task<OrderDto> UpdateOrderStatusAsync(int orderId, OrderStatus status);
    Task<KOTDto> GenerateKOTAsync(int orderId, string notes = "");
    Task<bool> CancelOrderAsync(int orderId, string reason = "");
}
