namespace FileManager.Application.DTOs;

public class VersioningSettingsDto
{
    public bool Enabled { get; set; } = true;
    public int MaxVersionsPerFile { get; set; } = 10;
    public int RetentionDays { get; set; } = 365;
}
