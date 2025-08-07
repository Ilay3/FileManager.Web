namespace FileManager.Application.DTOs;

public class CleanupSettingsDto
{
    public int TrashRetentionDays { get; set; } = 30;
    public int ArchiveCleanupDays { get; set; } = 365;
}
