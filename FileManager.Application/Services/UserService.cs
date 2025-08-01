using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace FileManager.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public UserService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<User?> ValidateUserAsync(string email, string password, string? ipAddress = null)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            return null;
        }

        // Проверяем блокировку аккаунта
        if (user.IsLocked)
        {
            return null;
        }

        // Проверяем активность
        if (!user.IsActive)
        {
            return null;
        }

        // Проверяем количество неудачных попыток
        var maxAttempts = int.Parse(_configuration["Security:MaxLoginAttempts"] ?? "5");
        var lockoutMinutes = int.Parse(_configuration["Security:LockoutMinutes"] ?? "30");

        if (user.FailedLoginAttempts >= maxAttempts &&
            user.LastFailedLoginAt.HasValue &&
            user.LastFailedLoginAt.Value.AddMinutes(lockoutMinutes) > DateTime.UtcNow)
        {
            return null; // Временная блокировка
        }

        // Проверяем пароль
        var passwordHash = HashPassword(password);
        if (user.PasswordHash != passwordHash)
        {
            // Увеличиваем счетчик неудачных попыток
            user.FailedLoginAttempts++;
            user.LastFailedLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return null;
        }

        // Успешный вход - сбрасываем счетчики и обновляем данные
        user.FailedLoginAttempts = 0;
        user.LastFailedLoginAt = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastActivityAt = DateTime.UtcNow;
        user.LastIpAddress = ipAddress;

        await _userRepository.UpdateAsync(user);

        return user;
    }

    public async Task<User> CreateUserAsync(string email, string fullName, string password, string? department = null, bool isAdmin = false)
    {
        if (await _userRepository.ExistsAsync(email))
            throw new InvalidOperationException($"Пользователь с email {email} уже существует");

        // Валидируем пароль
        ValidatePassword(password);

        var user = new User
        {
            Email = email,
            FullName = fullName,
            PasswordHash = HashPassword(password),
            Department = department,
            IsActive = true,
            IsAdmin = isAdmin,
            LastActivityAt = DateTime.UtcNow
        };

        return await _userRepository.CreateAsync(user);
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _userRepository.ExistsAsync(email);
    }

    public async Task<bool> LockUserAsync(Guid userId, string reason, Guid lockedById)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        user.IsLocked = true;
        user.LockedAt = DateTime.UtcNow;
        user.LockReason = reason;
        user.LockedById = lockedById;

        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> UnlockUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        user.IsLocked = false;
        user.LockedAt = null;
        user.LockReason = null;
        user.LockedById = null;
        user.FailedLoginAttempts = 0;

        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || !user.IsActive || user.IsLocked)
            return null;

        var token = GenerateSecureToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1); // Токен действует 1 час

        await _userRepository.UpdateAsync(user);
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.PasswordResetToken != token ||
            user.PasswordResetTokenExpires < DateTime.UtcNow)
            return false;

        // Валидируем новый пароль
        ValidatePassword(newPassword);

        user.PasswordHash = HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpires = null;
        user.PasswordResetAt = DateTime.UtcNow;
        user.FailedLoginAttempts = 0;

        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task UpdateLastActivityAsync(Guid userId, string? ipAddress = null)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            user.LastActivityAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(ipAddress))
                user.LastIpAddress = ipAddress;

            await _userRepository.UpdateAsync(user);
        }
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "FileManagerSalt"));
        return Convert.ToBase64String(hashedBytes);
    }

    private void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Пароль не может быть пустым");

        if (password.Length < 8)
            throw new ArgumentException("Пароль должен содержать минимум 8 символов");

        if (!password.Any(char.IsUpper))
            throw new ArgumentException("Пароль должен содержать минимум одну заглавную букву");

        if (!password.Any(char.IsLower))
            throw new ArgumentException("Пароль должен содержать минимум одну строчную букву");

        if (!password.Any(char.IsDigit))
            throw new ArgumentException("Пароль должен содержать минимум одну цифру");

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            throw new ArgumentException("Пароль должен содержать минимум один специальный символ");
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}