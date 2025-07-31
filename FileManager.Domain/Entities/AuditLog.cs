using FileManager.Domain.Common;
using FileManager.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Domain.Entities;

public class AuditLog : BaseEntity
{
    public AuditAction Action { get; set; }

    // Кто выполнил действие
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }

    // Над каким объектом (файл ИЛИ папка)
    public Guid? FileId { get; set; }
    public virtual Files? File { get; set; }

    public Guid? FolderId { get; set; }
    public virtual Folder? Folder { get; set; }

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty; // детальное описание

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public bool IsSuccess { get; set; } = true; // успешно или ошибка

    [StringLength(1000)]
    public string? ErrorMessage { get; set; } // если ошибка
}
