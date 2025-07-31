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

    // Navigation properties
    public virtual ICollection<Files> UploadedFiles { get; set; } = new List<Files>();
    public virtual ICollection<Folder> CreatedFolders { get; set; } = new List<Folder>();
    public virtual ICollection<FileVersion> FileVersions { get; set; } = new List<FileVersion>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
}
