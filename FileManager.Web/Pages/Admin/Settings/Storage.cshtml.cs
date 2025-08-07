using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;

namespace FileManager.Web.Pages.Admin.Settings;

[Authorize]
public class StorageModel : PageModel
{
    private readonly ISettingsService _settingsService;

    public StorageModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public string ArchivePath { get; set; } = string.Empty;

    [BindProperty]
    public long MaxFileSize { get; set; }

    [BindProperty]
    public string AllowedExtensions { get; set; } = string.Empty;

    [BindProperty]
    public long QuotaPerUser { get; set; }

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        var options = await _settingsService.GetStorageOptionsAsync();
        ArchivePath = options.ArchivePath;
        MaxFileSize = options.MaxFileSize;
        AllowedExtensions = string.Join(", ", options.AllowedExtensions);
        QuotaPerUser = options.QuotaPerUser;
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

        var current = await _settingsService.GetStorageOptionsAsync();
        current.ArchivePath = ArchivePath;
        current.MaxFileSize = MaxFileSize;
        current.AllowedExtensions = AllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToArray();
        current.QuotaPerUser = QuotaPerUser;
        await _settingsService.SaveStorageOptionsAsync(current);
        TempData["Success"] = "Настройки сохранены";
        return RedirectToPage();
    }
}
