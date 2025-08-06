using FileManager.Application.Services;
using FileManager.Application.Interfaces;
using FileManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FileManager.Web.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterModel> _logger;

    [BindProperty]
    public RegisterViewModel RegisterData { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public RegisterModel(UserService userService, IEmailService emailService, ILogger<RegisterModel> logger)
    {
        _userService = userService;
        _emailService = emailService;
        _logger = logger;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var user = await _userService.CreateUserAsync(RegisterData.Email, RegisterData.FullName, RegisterData.Password, RegisterData.Department, false);
            await _emailService.SendEmailConfirmationAsync(user.Email, user.FullName, user.EmailConfirmationCode!);
            return RedirectToPage("/Account/ConfirmEmail", new { email = user.Email });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error {Email}", RegisterData.Email);
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
