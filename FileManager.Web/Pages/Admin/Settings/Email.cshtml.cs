using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileManager.Web.Pages.Admin.Settings;

[Authorize]
public class EmailModel : PageModel
{
    private readonly ISettingsService _settingsService;

    public EmailModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public string SmtpServer { get; set; } = string.Empty;

    [BindProperty]
    public int SmtpPort { get; set; }

    [BindProperty]
    public bool EnableSsl { get; set; }

    [BindProperty]
    public bool Enabled { get; set; }

    [BindProperty]
    public string PasswordResetTemplate { get; set; } = string.Empty;

    [BindProperty]
    public string AccountLockedTemplate { get; set; } = string.Empty;

    [BindProperty]
    public string WelcomeTemplate { get; set; } = string.Empty;

    [BindProperty]
    public string EmailConfirmationTemplate { get; set; } = string.Empty;

    [BindProperty]
    public string TestEmail { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        var options = await _settingsService.GetEmailOptionsAsync();
        SmtpServer = options.SmtpServer;
        SmtpPort = options.SmtpPort;
        EnableSsl = options.EnableSsl;
        Enabled = options.Enabled;
        PasswordResetTemplate = options.PasswordResetTemplate;
        AccountLockedTemplate = options.AccountLockedTemplate;
        WelcomeTemplate = options.WelcomeTemplate;
        EmailConfirmationTemplate = options.EmailConfirmationTemplate;
        TestEmail = options.TestEmail;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            return Redirect("/Files");
        }

        var current = await _settingsService.GetEmailOptionsAsync();
        current.SmtpServer = SmtpServer;
        current.SmtpPort = SmtpPort;
        current.EnableSsl = EnableSsl;
        current.Enabled = Enabled;
        current.PasswordResetTemplate = PasswordResetTemplate;
        current.AccountLockedTemplate = AccountLockedTemplate;
        current.WelcomeTemplate = WelcomeTemplate;
        current.EmailConfirmationTemplate = EmailConfirmationTemplate;
        current.TestEmail = TestEmail;
        await _settingsService.SaveEmailOptionsAsync(current);
        TempData["Success"] = "Настройки сохранены";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostTestAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            return Redirect("/Files");
        }

        var options = await _settingsService.GetEmailOptionsAsync();
        options.TestEmail = TestEmail;
        var result = await _settingsService.SendTestEmailAsync(options);
        if (result)
            TempData["Success"] = "Тестовое письмо отправлено";
        else
            TempData["Error"] = "Не удалось отправить тестовое письмо";
        return RedirectToPage();
    }
}
