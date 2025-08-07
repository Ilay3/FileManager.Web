using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileManager.Web.Pages.Admin.Settings;

[Authorize]
public class SecurityModel : PageModel
{
    private readonly ISettingsService _settingsService;

    public SecurityModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public bool RequireTwoFactor { get; set; }

    [BindProperty]
    public string PasswordComplexity { get; set; } = "Medium";

    [BindProperty]
    public int MaxLoginAttempts { get; set; }

    [BindProperty]
    public int LockoutMinutes { get; set; }

    [BindProperty]
    public int SessionTimeoutMinutes { get; set; }

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        var options = await _settingsService.GetSecurityOptionsAsync();
        RequireTwoFactor = options.RequireTwoFactor;
        PasswordComplexity = options.PasswordComplexity;
        MaxLoginAttempts = options.MaxLoginAttempts;
        LockoutMinutes = options.LockoutMinutes;
        SessionTimeoutMinutes = options.SessionTimeoutMinutes;
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

        var current = await _settingsService.GetSecurityOptionsAsync();
        current.RequireTwoFactor = RequireTwoFactor;
        current.PasswordComplexity = PasswordComplexity;
        current.MaxLoginAttempts = MaxLoginAttempts;
        current.LockoutMinutes = LockoutMinutes;
        current.SessionTimeoutMinutes = SessionTimeoutMinutes;

        if (!_settingsService.ValidateSecurityOptions(current))
        {
            ModelState.AddModelError(string.Empty, "Недопустимые значения безопасности");
            return Page();
        }

        await _settingsService.SaveSecurityOptionsAsync(current);
        TempData["Success"] = "Настройки сохранены";
        return RedirectToPage();
    }
}
