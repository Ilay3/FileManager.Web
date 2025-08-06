using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace FileManager.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, IConfiguration configuration, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<User?> ValidateUserAsync(string email, string password, string? ipAddress = null)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            _logger.LogWarning("Попытка входа с несуществующим email {Email}", email);
            return null;
        }

        // Проверяем блокировку аккаунта
        if (user.IsLocked)
        {
            _logger.LogWarning("Пользователь {Email} заблокирован", email);
            return null;
        }

        // Проверяем активность
        if (!user.IsActive)
        {
            _logger.LogWarning("Пользователь {Email} не активен", email);
            return null;
        }

        if (!user.IsEmailConfirmed)
        {
            _logger.LogWarning("Пользователь {Email} не подтвердил email", email);
            return null;
        }

        // Проверяем количество неудачных попыток
        var maxAttempts = int.Parse(_configuration["Security:MaxLoginAttempts"] ?? "5");
        var lockoutMinutes = int.Parse(_configuration["Security:LockoutMinutes"] ?? "30");

        if (user.FailedLoginAttempts >= maxAttempts &&
            user.LastFailedLoginAt.HasValue &&
            user.LastFailedLoginAt.Value.AddMinutes(lockoutMinutes) > DateTime.UtcNow)
        {
            _logger.LogWarning("Пользователь {Email} временно заблокирован из-за неудачных попыток", email);
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
            _logger.LogWarning("Неверный пароль для {Email}", email);
            return null;
        }

        // Успешный вход - сбрасываем счетчики и обновляем данные
        user.FailedLoginAttempts = 0;
        user.LastFailedLoginAt = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastActivityAt = DateTime.UtcNow;
        user.LastIpAddress = ipAddress;

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Пользователь {Email} успешно вошёл", email);

        return user;
    }

    public async Task<User> CreateUserAsync(string email, string fullName, string password, string? department = null, bool isAdmin = false)
    {
        if (await _userRepository.ExistsAsync(email))
        {
            _logger.LogWarning("Попытка создания уже существующего пользователя {Email}", email);
            throw new InvalidOperationException($"Пользователь с email {email} уже существует");
        }

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
            LastActivityAt = DateTime.UtcNow,
            IsEmailConfirmed = false,
            EmailConfirmationCode = GenerateConfirmationCode()
        };

        var created = await _userRepository.CreateAsync(user);
        _logger.LogInformation("Создан новый пользователь {Email}", email);
        return created;
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
        {
            _logger.LogWarning("Запрос на сброс пароля для недоступного пользователя {Email}", email);
            return null;
        }

        var token = GenerateSecureToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1); // Токен действует 1 час

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Сгенерирован токен сброса пароля для {Email}", email);
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.PasswordResetToken != token ||
            user.PasswordResetTokenExpires < DateTime.UtcNow)
        {
            _logger.LogWarning("Неудачная попытка сброса пароля для {Email}", email);
            return false;
        }

        // Валидируем новый пароль
        ValidatePassword(newPassword);

        user.PasswordHash = HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpires = null;
        user.PasswordResetAt = DateTime.UtcNow;
        user.FailedLoginAttempts = 0;

        await _userRepository.UpdateAsync(user);
        _logger.LogInformation("Пароль пользователя {Email} успешно изменён", email);
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

    private string GenerateConfirmationCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var code = BitConverter.ToUInt32(bytes) % 1000000;
        return code.ToString("D6");
    }

    public async Task<bool> ConfirmEmailAsync(string email, string code)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.EmailConfirmationCode != code)
            return false;

        user.IsEmailConfirmed = true;
        user.EmailConfirmationCode = null;
        await _userRepository.UpdateAsync(user);
        return true;
    }
}