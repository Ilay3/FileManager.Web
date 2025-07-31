namespace FileManager.Infrastructure.Configuration;

public class SecurityOptions
{
    public const string SectionName = "Security";

    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 30;
    public int SessionTimeoutMinutes { get; set; } = 480;
}
