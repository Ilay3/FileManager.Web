namespace FileManager.Infrastructure.Configuration;

public class UploadSecurityOptions
{
    public const string SectionName = "UploadSecurity";

    public bool EnableAntivirus { get; set; } = false;

    public int UserQuotaMb { get; set; } = 0;

    public string[] BlockedExtensions { get; set; } = Array.Empty<string>();
}

