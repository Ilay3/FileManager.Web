using FileManager.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Domain.Entities;

public class FileEditSession : BaseEntity
{
    public Guid FileId { get; set; }
    public virtual Files File { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [StringLength(1000)]
    public string? YandexEditUrl { get; set; } // URL для редактирования в Яндекс.Документах

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public bool IsActive => EndedAt == null && StartedAt > DateTime.UtcNow.AddMinutes(-30); // Активна, если не закрыта и начата не более 30 минут назад
}