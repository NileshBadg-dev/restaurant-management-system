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
    
    // Draft Operations (OM-A03, OM-A04, OM-I03)
    Task<OrderDto> SaveDraftAsync(SaveDraftOrderDto dto);
    Task<OrderDto?> GetDraftAsync(int? tableId, int? userId = null);
    Task<bool> DiscardDraftAsync(int? tableId, int? userId = null, string? managerPin = null);

    // Items and KOTs (FR-ORD-05, FR-OM-01 to FR-OM-10)
    Task<OrderDto> AddItemToOrderAsync(int orderId, CreateOrderItemDto dto);
    Task<OrderDto> RemoveItemFromOrderAsync(int orderId, int orderItemId);
    Task<OrderDto> UpdateOrderStatusAsync(int orderId, OrderStatus status);
    Task<KOTDto> GenerateKOTAsync(int orderId, string notes = "");
    Task<KOTDto> SendKOTAsync(int orderId, List<CreateOrderItemDto>? newItems = null, string notes = "", bool isRush = false);
    
    // In-Flight Order Operations (Section 5B)
    Task<OrderDto> ReduceItemQuantityAsync(ReduceItemDto dto);
    Task<OrderDto> ReplaceItemAsync(ReplaceItemDto dto);
    Task<OrderDto> ChangeItemNoteAsync(ChangeItemNoteDto dto);
    Task<OrderDto> UpdateItemStatusAsync(int orderId, int orderItemId, OrderItemStatus status);
    Task<OrderDto> MarkAllServedAsync(int orderId);
    Task<OrderDto> MakeItemComplimentaryAsync(ComplimentaryItemDto dto);
    Task<OrderDto> ReturnItemAsync(ReturnItemDto dto);

    // Table & Party Scenarios (Section 5E)
    Task<bool> TransferTableAsync(int sourceTableId, int targetTableId);
    Task<bool> MergeTablesAsync(int sourceTableId, int targetTableId);
    Task<bool> MarkWalkoutAsync(WalkoutOrderDto dto);

    // Cancel and Reopen (Section 5F, 5J)
    Task<bool> CancelOrderAsync(int orderId, string reason = "", string? managerPin = null);
    Task<OrderDto> ReopenOrderAsync(int orderId, string reason, string? managerPin = null);

    // Billing Hand-off (Section 5H)
    Task<BillDto> GenerateBillFromPOSAsync(int orderId, decimal discountAmount = 0m, string? discountReason = null, string? customerGstin = null);
}
