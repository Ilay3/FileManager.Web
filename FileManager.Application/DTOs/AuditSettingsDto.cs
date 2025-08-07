namespace FileManager.Application.DTOs;

public class AuditSettingsDto
{
    public bool EnableFileActions { get; set; } = true;
    public bool EnableUserActions { get; set; } = true;
    public bool EnableAccessLog { get; set; } = true;
    public int RetentionDays { get; set; } = 365;
    public string LogLevel { get; set; } = "Information";
}
