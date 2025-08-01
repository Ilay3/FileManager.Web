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

    public IActionResult OnGet(string? token, string? email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            ErrorMessage = "Неверная ссылка сброса пароля.";
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
            var success = await _userService.ResetPasswordAsync(
                ResetData.Email,
                ResetData.Token,
                ResetData.NewPassword);

            if (success)
            {
                Message = "Пароль успешно изменен! Теперь вы можете войти в систему с новым паролем.";
                _logger.LogInformation("Пароль успешно сброшен для пользователя {Email}", ResetData.Email);

                // Очищаем форму
                ResetData = new PasswordResetViewModel();
            }
            else
            {
                ErrorMessage = "Ссылка для сброса пароля недействительна или устарела. Запросите новую ссылку.";
                _logger.LogWarning("Попытка сброса пароля с недействительным токеном для {Email}", ResetData.Email);
            }
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogWarning("Ошибка валидации пароля для {Email}: {Error}", ResetData.Email, ex.Message);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Произошла ошибка при сбросе пароля. Попробуйте позже.";
            _logger.LogError(ex, "Ошибка при сбросе пароля для {Email}", ResetData.Email);
        }

        return Page();
    }
}