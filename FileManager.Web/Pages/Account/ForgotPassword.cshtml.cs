using FileManager.Application.Services;
using FileManager.Application.Interfaces;
using FileManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileManager.Web.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(UserService userService, IEmailService emailService, ILogger<ForgotPasswordModel> logger)
    {
        _userService = userService;
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public PasswordResetRequestViewModel ResetRequest { get; set; } = new();

    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        // Просто показываем форму
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Генерируем токен сброса (даже если пользователь не найден, для безопасности)
            var resetToken = await _userService.GeneratePasswordResetTokenAsync(ResetRequest.Email);

            if (resetToken != null)
            {
                // Получаем пользователя для отправки email
                var user = await _userService.GetUserByEmailAsync(ResetRequest.Email);
                if (user != null)
                {
                    await _emailService.SendPasswordResetEmailAsync(ResetRequest.Email, resetToken, user.FullName);
                    _logger.LogInformation("Отправлен запрос сброса пароля для {Email}", ResetRequest.Email);
                }
            }

            // Всегда показываем успешное сообщение (для безопасности)
            Message = "Если указанный email существует в системе, на него будет отправлена ссылка для сброса пароля.";
            ResetRequest.Email = string.Empty; // Очищаем поле
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке запроса сброса пароля для {Email}", ResetRequest.Email);
            ErrorMessage = "Произошла ошибка при обработке запроса. Попробуйте позже.";
        }

        return Page();
    }
}