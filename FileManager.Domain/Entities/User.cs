using FileManager.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Domain.Entities;

public class User : BaseEntity
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    // Поля для блокировки аккаунта
    public bool IsLocked { get; set; } = false;
    public DateTime? LockedAt { get; set; }
    public string? LockReason { get; set; }
    public Guid? LockedById { get; set; }

    // Поля для попыток входа
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LastFailedLoginAt { get; set; }

    // Поля для сброса пароля
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }
    public DateTime? PasswordResetAt { get; set; }

    // Поля для последней активности
    public DateTime? LastActivityAt { get; set; }
    public string? LastIpAddress { get; set; }

    public bool IsEmailConfirmed { get; set; } = false;

    [StringLength(10)]
    public string? EmailConfirmationCode { get; set; }

    // Navigation properties
    public virtual ICollection<Files> UploadedFiles { get; set; } = new List<Files>();
    public virtual ICollection<Folder> CreatedFolders { get; set; } = new List<Folder>();
    public virtual ICollection<FileVersion> FileVersions { get; set; } = new List<FileVersion>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}