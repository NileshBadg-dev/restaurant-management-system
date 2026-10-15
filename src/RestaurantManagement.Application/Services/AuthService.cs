using RestaurantManagement.Application.DTOs;
using RestaurantManagement.Application.Interfaces;
using RestaurantManagement.Domain.Entities;
using RestaurantManagement.Domain.Enums;

namespace RestaurantManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.Username.ToLower() == request.Username.ToLower() && u.IsActive && !u.IsDeleted);
        var user = users.FirstOrDefault();

        if (user == null || user.PasswordHash != request.Password) // Simplified password check for demo/seed
        {
            return new LoginResponseDto
            {
                Success = false,
                Message = "Invalid username or password"
            };
        }

        user.LastLogin = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var userDto = MapToUserDto(user);
        
        // Mock JWT Token string (or real token in API controller)
        string token = $"JWT-TOKEN-USER-{user.Id}-{user.Role}";

        return new LoginResponseDto
        {
            Success = true,
            Message = "Login successful",
            Token = token,
            User = userDto
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user != null ? MapToUserDto(user) : null;
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        var users = await _unitOfWork.Users.FindAsync(u => u.Username.ToLower() == username.Trim().ToLower() && !u.IsDeleted);
        var user = users.FirstOrDefault();
        return user != null ? MapToUserDto(user) : null;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return users.Where(u => !u.IsDeleted).Select(MapToUserDto);
    }

    public async Task<bool> ResetPasswordByUsernameAsync(string username, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(newPassword)) return false;
        var users = await _unitOfWork.Users.FindAsync(u => u.Username.ToLower() == username.Trim().ToLower() && !u.IsDeleted);
        var user = users.FirstOrDefault();
        if (user == null) return false;

        user.PasswordHash = newPassword;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(PasswordChangeDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
        if (user == null || user.PasswordHash != dto.OldPassword) return false;

        user.PasswordHash = dto.NewPassword;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLogin = user.LastLogin
        };
    }
}
