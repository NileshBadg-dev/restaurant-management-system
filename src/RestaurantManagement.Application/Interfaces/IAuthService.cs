using RestaurantManagement.Application.DTOs;

namespace RestaurantManagement.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<bool> ChangePasswordAsync(PasswordChangeDto dto);
    Task<bool> ResetPasswordByUsernameAsync(string username, string newPassword);
    Task<bool> ToggleUserStatusAsync(int userId);
}
