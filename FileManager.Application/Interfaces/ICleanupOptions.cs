namespace FileManager.Application.Interfaces;

public interface ICleanupOptions
{
    int TrashRetentionDays { get; }
    int ArchiveCleanupDays { get; }
}
