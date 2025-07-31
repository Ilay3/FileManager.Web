using FileManager.Domain.Common;
using FileManager.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Domain.Entities;

public class Files : BaseEntity
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string OriginalName { get; set; } = string.Empty; // оригинальное имя при загрузке

    [Required]
    [StringLength(1000)]
    public string YandexPath { get; set; } = string.Empty; // полный путь в Яндекс.Диске

    public FileType FileType { get; set; }

    [Required]
    [StringLength(10)]
    public string Extension { get; set; } = string.Empty; // .docx, .pdf, .jpg

    public long SizeBytes { get; set; }

    // Связь с папкой
    public Guid FolderId { get; set; }
    public virtual Folder Folder { get; set; } = null!;

    // Кто загрузил
    public Guid UploadedById { get; set; }
    public virtual User UploadedBy { get; set; } = null!;

    // Текущая активная версия
    public Guid? CurrentVersionId { get; set; }
    public virtual FileVersion? CurrentVersion { get; set; }

    // Теги и метки (как указано в ТЗ)
    [StringLength(500)]
    public string? Tags { get; set; } // JSON массив тегов

    // Navigation properties
    public virtual ICollection<FileVersion> Versions { get; set; } = new List<FileVersion>();
    public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
