using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Application.Interfaces;

public interface IReservationService
{
    Task<IEnumerable<ReservationDto>> GetReservationsAsync(DateTime? date = null, ReservationStatus? status = null);
    Task<ReservationDto> CreateReservationAsync(CreateReservationDto dto);
    Task<ReservationDto> UpdateReservationStatusAsync(int id, ReservationStatus status);
}
