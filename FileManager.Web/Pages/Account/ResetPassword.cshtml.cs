using FileManager.Application.Services;
using FileManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileManager.Web.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly UserService _userService;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(UserService userService, ILogger<ResetPasswordModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [BindProperty]
    public PasswordResetViewModel ResetData { get; set; } = new();

    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string? token = null, string? email = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Files/Index");
        }

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            ErrorMessage = "Недействительная ссылка для сброса пароля.";
            return Page();
        }

        ResetData.Token = token;
        ResetData.Email = email;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var success = await _userService.ResetPasswordAsync(ResetData.Email, ResetData.Token, ResetData.NewPassword);

            if (success)
            {
                Message = "Пароль успешно изменен! Теперь вы можете войти в систему с новым паролем.";
                _logger.LogInformation("Пароль успешно сброшен для {Email}", ResetData.Email);
                return Page();
            }
            else
            {
                ErrorMessage = "Недействительный или просроченный токен сброса пароля.";
                return Page();
            }
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сбросе пароля для {Email}", ResetData.Email);
            ErrorMessage = "Произошла ошибка при сбросе пароля. Попробуйте еще раз.";
            return Page();
        }
    }
}