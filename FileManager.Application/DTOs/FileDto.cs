using FileManager.Domain.Enums;

namespace FileManager.Application.DTOs;

public class FileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Tags { get; set; }

    // Folder info
    public Guid FolderId { get; set; }
    public string FolderName { get; set; } = string.Empty;

    // Upload info
    public Guid UploadedById { get; set; }
    public string UploadedByName { get; set; } = string.Empty;

    // Helper properties
    public string FormattedSize => FormatFileSize(SizeBytes);
    public string FileIcon => GetFileIcon();

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} Б";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} КБ";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} МБ";
        return $"{bytes / (1024 * 1024 * 1024):F1} ГБ";
    }

    private string GetFileIcon()
    {
        return FileType switch
        {
            FileType.Document => "📄",
            FileType.Spreadsheet => "📊",
            FileType.Presentation => "📽️",
            FileType.Pdf => "📕",
            FileType.Image => "🖼️",
            FileType.Text => "📝",
            FileType.Archive => "📦",
            _ => "📄"
        };
    }
}