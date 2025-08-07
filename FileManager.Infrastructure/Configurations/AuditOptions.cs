using Microsoft.Extensions.Logging;

namespace FileManager.Infrastructure.Configuration;

public class AuditOptions
{
    public const string SectionName = "Audit";

    public bool EnableFileActions { get; set; } = true;
    public bool EnableUserActions { get; set; } = true;
    public bool EnableAccessLog { get; set; } = true;
    public int RetentionDays { get; set; } = 365;
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
