namespace FileManager.Application.DTOs;

public class ThemeSettingsDto
{
    public string Theme { get; set; } = "light";
    public string LogoUrl { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#0d6efd";
}
