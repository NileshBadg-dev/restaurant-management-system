using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Application.Interfaces;

public interface ITableService
{
    Task<IEnumerable<FloorDto>> GetFloorsWithTablesAsync();
    Task<IEnumerable<TableDto>> GetAllTablesAsync();
    Task<TableDto?> GetTableByIdAsync(int id);
    Task<TableDto> CreateTableAsync(TableCreateUpdateDto dto);
    Task<TableDto> UpdateTableAsync(int id, TableCreateUpdateDto dto);
    Task<bool> DeleteTableAsync(int id);
    Task<bool> UpdateTableStatusAsync(int id, TableStatus status);
    Task<bool> TransferTableAsync(int sourceTableId, int targetTableId);
    Task<bool> MergeTablesAsync(int sourceTableId, int targetTableId);
    Task<FloorDto> CreateFloorAsync(FloorCreateUpdateDto dto);
}
