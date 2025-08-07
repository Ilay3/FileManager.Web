namespace FileManager.Infrastructure.Configuration;

public class CleanupOptions
{
    public const string SectionName = "Cleanup";

    public int TrashRetentionDays { get; set; } = 30;
    public int ArchiveCleanupDays { get; set; } = 365;
}
