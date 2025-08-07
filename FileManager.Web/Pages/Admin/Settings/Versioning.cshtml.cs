using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileManager.Web.Pages.Admin.Settings;

[Authorize]
public class VersioningModel : PageModel
{
    private readonly ISettingsService _settingsService;

    public VersioningModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public bool Enabled { get; set; }

    [BindProperty]
    public int MaxVersionsPerFile { get; set; }

    [BindProperty]
    public int RetentionDays { get; set; }

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        var options = await _settingsService.GetVersioningOptionsAsync();
        Enabled = options.Enabled;
        MaxVersionsPerFile = options.MaxVersionsPerFile;
        RetentionDays = options.RetentionDays;
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

        var current = await _settingsService.GetVersioningOptionsAsync();
        current.Enabled = Enabled;
        current.MaxVersionsPerFile = MaxVersionsPerFile;
        current.RetentionDays = RetentionDays;

        await _settingsService.SaveVersioningOptionsAsync(current);
        TempData["Success"] = "Настройки сохранены";
        return RedirectToPage();
    }
}
