using FileManager.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Domain.Entities;

public class FileVersion : BaseEntity
{
    public Guid FileId { get; set; }
    public virtual Files File { get; set; } = null!;

    public int VersionNumber { get; set; } // 1, 2, 3...

    [StringLength(1000)]
    public string? LocalArchivePath { get; set; } // /archive/{file_id}/{yyyy-MM-dd_HH-mm-ss}_{user}.docx

    public long SizeBytes { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; } // комментарий при загрузке/изменении

    // Кто создал эту версию
    public Guid CreatedById { get; set; }
    public virtual User CreatedBy { get; set; } = null!;

    public bool IsCurrentVersion { get; set; } = false;
}
