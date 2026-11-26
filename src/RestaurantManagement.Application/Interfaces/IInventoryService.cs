using RestaurantManagement.Application.DTOs;

namespace RestaurantManagement.Application.Interfaces;

public interface IInventoryService
{
    Task<IEnumerable<InventoryItemDto>> GetInventoryItemsAsync();
    Task<IEnumerable<InventoryItemDto>> GetLowStockItemsAsync();
    Task<InventoryItemDto> AdjustStockAsync(StockAdjustmentDto dto);
    Task DeductStockForOrderAsync(int orderId);
}
