using FileManager.Application.Services;
using FileManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FileManager.Web.Pages.Account;

public class ConfirmEmailModel : PageModel
{
    private readonly UserService _userService;
    private readonly ILogger<ConfirmEmailModel> _logger;

    public ConfirmEmailModel(UserService userService, ILogger<ConfirmEmailModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [BindProperty]
    public EmailConfirmationViewModel Input { get; set; } = new();

    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet(string email)
    {
        Input.Email = email;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var success = await _userService.ConfirmEmailAsync(Input.Email, Input.Code);
            if (success)
            {
                _logger.LogInformation("Email подтвержден {Email}", Input.Email);
                return RedirectToPage("/Account/Login");
            }

            ErrorMessage = "Неверный код подтверждения";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка подтверждения email {Email}", Input.Email);
            ErrorMessage = "Произошла ошибка. Попробуйте позже.";
            return Page();
        }
    }
}
