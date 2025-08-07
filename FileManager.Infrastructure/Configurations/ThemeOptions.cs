namespace FileManager.Infrastructure.Configuration;

public class ThemeOptions
{
    public const string SectionName = "Theme";

    public string Theme { get; set; } = "light";
    public string LogoUrl { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#0d6efd";
}
