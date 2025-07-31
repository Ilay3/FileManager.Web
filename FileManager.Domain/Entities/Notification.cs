using FileManager.Domain.Common;
using FileManager.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public AuditAction RelatedAction { get; set; }

    // Связанные объекты
    public Guid? FileId { get; set; }
    public virtual Files? File { get; set; }

    public Guid? FolderId { get; set; }
    public virtual Folder? Folder { get; set; }

    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }
}
