using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileManager.Web.Pages.Admin.Settings;

[Authorize]
public class AuditModel : PageModel
{
    private readonly ISettingsService _settingsService;

    public AuditModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public bool EnableFileActions { get; set; }

    [BindProperty]
    public bool EnableUserActions { get; set; }

    [BindProperty]
    public bool EnableAccessLog { get; set; }

    [BindProperty]
    public int RetentionDays { get; set; }

    [BindProperty]
    public string LogLevel { get; set; } = "Information";

    public List<string> LogLevels { get; } = Enum.GetNames(typeof(Microsoft.Extensions.Logging.LogLevel)).ToList();

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        var options = await _settingsService.GetAuditOptionsAsync();
        EnableFileActions = options.EnableFileActions;
        EnableUserActions = options.EnableUserActions;
        EnableAccessLog = options.EnableAccessLog;
        RetentionDays = options.RetentionDays;
        LogLevel = options.LogLevel;
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

        var current = await _settingsService.GetAuditOptionsAsync();
        current.EnableFileActions = EnableFileActions;
        current.EnableUserActions = EnableUserActions;
        current.EnableAccessLog = EnableAccessLog;
        current.RetentionDays = RetentionDays;
        current.LogLevel = LogLevel;

        await _settingsService.SaveAuditOptionsAsync(current);
        TempData["Success"] = "Настройки сохранены";
        return RedirectToPage();
    }
}
