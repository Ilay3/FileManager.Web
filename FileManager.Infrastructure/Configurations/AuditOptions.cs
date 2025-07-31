public class AuditOptions
{
    public const string SectionName = "Audit";

    public bool EnableFileActions { get; set; } = true;
    public bool EnableUserActions { get; set; } = true;
    public int RetentionDays { get; set; } = 365;
}
