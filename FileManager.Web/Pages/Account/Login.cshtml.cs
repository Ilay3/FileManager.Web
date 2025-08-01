using FileManager.Application.Services;
using FileManager.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace FileManager.Web.Pages.Account;

public class LoginModel : PageModel
{
    private readonly UserService _userService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(UserService userService, ILogger<LoginModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [BindProperty]
    public LoginViewModel LoginData { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        // Если пользователь уже авторизован, перенаправляем
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/Files");
        }

        LoginData.ReturnUrl = returnUrl;
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
            // Получаем IP адрес для логирования
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Валидируем пользователя
            var user = await _userService.ValidateUserAsync(LoginData.Email, LoginData.Password);

            if (user == null)
            {
                ErrorMessage = "Неверный email или пароль";
                _logger.LogWarning("Неудачная попытка входа для {Email} с IP {IP}",
                    LoginData.Email, ipAddress);
                return Page();
            }

            // Проверяем, не заблокирован ли аккаунт
            if (!user.IsActive)
            {
                ErrorMessage = "Ваш аккаунт заблокирован. Обратитесь к администратору.";
                _logger.LogWarning("Попытка входа заблокированного пользователя {Email}", LoginData.Email);
                return Page();
            }

            // Создаем claims для авторизации
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new("IsAdmin", user.IsAdmin.ToString()),
                new("Department", user.Department ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = LoginData.RememberMe,
                ExpiresUtc = LoginData.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("Успешный вход пользователя {Email} ({UserId})",
                user.Email, user.Id);

            // Перенаправляем пользователя
            if (!string.IsNullOrEmpty(LoginData.ReturnUrl) && Url.IsLocalUrl(LoginData.ReturnUrl))
            {
                return Redirect(LoginData.ReturnUrl);
            }

            return Redirect("/Files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при входе пользователя {Email}", LoginData.Email);
            ErrorMessage = "Произошла ошибка при входе в систему. Попробуйте позже.";
            return Page();
        }
    }
}