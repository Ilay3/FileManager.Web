using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileManager.Web.Pages.Admin.Settings;

[Authorize]
public class ThemeModel : PageModel
{
    private readonly ISettingsService _settingsService;

    public ThemeModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public string Theme { get; set; } = string.Empty;

    [BindProperty]
    public string LogoUrl { get; set; } = string.Empty;

    [BindProperty]
    public string AccentColor { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        var options = await _settingsService.GetThemeOptionsAsync();
        Theme = options.Theme;
        LogoUrl = options.LogoUrl;
        AccentColor = options.AccentColor;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            return Redirect("/Files");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var current = await _settingsService.GetThemeOptionsAsync();
        current.Theme = Theme;
        current.LogoUrl = LogoUrl;
        current.AccentColor = AccentColor;
        await _settingsService.SaveThemeOptionsAsync(current);
        TempData["Success"] = "Настройки сохранены";
        return RedirectToPage();
    }
}
