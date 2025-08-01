using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<List<UserDto>> GetUsersByDepartmentAsync(string department);
}