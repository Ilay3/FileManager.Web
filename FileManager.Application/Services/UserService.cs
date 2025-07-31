using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace FileManager.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null || !user.IsActive)
            return null;

        // Проверяем пароль
        var passwordHash = HashPassword(password);
        if (user.PasswordHash != passwordHash)
            return null;

        // Обновляем время последнего входа
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return user;
    }

    public async Task<User> CreateUserAsync(string email, string fullName, string password, string? department = null, bool isAdmin = false)
    {
        if (await _userRepository.ExistsAsync(email))
            throw new InvalidOperationException($"Пользователь с email {email} уже существует");

        var user = new User
        {
            Email = email,
            FullName = fullName,
            PasswordHash = HashPassword(password),
            Department = department,
            IsActive = true,
            IsAdmin = isAdmin
        };

        return await _userRepository.CreateAsync(user);
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _userRepository.ExistsAsync(email);
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "FileManagerSalt"));
        return Convert.ToBase64String(hashedBytes);
    }
}
