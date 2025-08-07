using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileManager.Web.Pages.Admin.Settings;

[Authorize]
public class CleanupModel : PageModel
{
    private readonly ISettingsService _settingsService;

    public CleanupModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public int TrashRetentionDays { get; set; }

    [BindProperty]
    public int ArchiveCleanupDays { get; set; }

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        var options = await _settingsService.GetCleanupOptionsAsync();
        TrashRetentionDays = options.TrashRetentionDays;
        ArchiveCleanupDays = options.ArchiveCleanupDays;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            return Redirect("/Files");
        }

        if (!ModelState.IsValid)
            return Page();

        var current = await _settingsService.GetCleanupOptionsAsync();
        current.TrashRetentionDays = TrashRetentionDays;
        current.ArchiveCleanupDays = ArchiveCleanupDays;
        await _settingsService.SaveCleanupOptionsAsync(current);
        TempData["Success"] = "Настройки сохранены";
        return RedirectToPage();
    }
}
