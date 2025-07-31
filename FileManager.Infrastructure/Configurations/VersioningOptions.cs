namespace FileManager.Infrastructure.Configuration;

public class VersioningOptions
{
    public const string SectionName = "Versioning";

    public int MaxVersionsPerFile { get; set; } = 10;
    public int RetentionDays { get; set; } = 365;
}
