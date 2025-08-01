using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;

namespace FileManager.Application.Services;

public class UserDtoService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserDtoService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<List<UserDto>> GetUsersByDepartmentAsync(string department)
    {
        var users = await _userRepository.GetAllAsync();
        return users.Where(u => u.Department == department).Select(MapToDto).ToList();
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Department = user.Department,
            IsActive = user.IsActive,
            IsAdmin = user.IsAdmin,
            IsLocked = user.IsLocked,
            LastLoginAt = user.LastLoginAt,
            LastActivityAt = user.LastActivityAt,
            CreatedAt = user.CreatedAt
        };
    }
}