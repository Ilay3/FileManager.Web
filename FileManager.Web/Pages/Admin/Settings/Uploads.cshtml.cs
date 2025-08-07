using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FileManager.Web.Pages.Admin.Settings;

[Authorize]
public class UploadsModel : PageModel
{
    private readonly ISettingsService _settingsService;

    public UploadsModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public bool EnableAntivirus { get; set; }

    [BindProperty]
    public int UserQuotaMb { get; set; }

    [BindProperty]
    public string BlockedExtensions { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        var options = await _settingsService.GetUploadSecurityOptionsAsync();
        EnableAntivirus = options.EnableAntivirus;
        UserQuotaMb = options.UserQuotaMb;
        BlockedExtensions = string.Join(", ", options.BlockedExtensions);
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

        var current = await _settingsService.GetUploadSecurityOptionsAsync();
        current.EnableAntivirus = EnableAntivirus;
        current.UserQuotaMb = UserQuotaMb;
        current.BlockedExtensions = BlockedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToArray();
        await _settingsService.SaveUploadSecurityOptionsAsync(current);
        TempData["Success"] = "Настройки сохранены";
        return RedirectToPage();
    }
}

