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

    public void OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            Response.Redirect("/Files");
            return;
        }

        LoginData.ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var user = await _userService.ValidateUserAsync(LoginData.Email, LoginData.Password,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            if (user == null)
            {
                ErrorMessage = "Неверный email или пароль, либо аккаунт заблокирован";
                return Page();
            }

            // Create claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new("IsAdmin", user.IsAdmin.ToString())
            };

            if (!string.IsNullOrEmpty(user.Department))
            {
                claims.Add(new("Department", user.Department));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = LoginData.RememberMe,
                ExpiresUtc = LoginData.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            _logger.LogInformation("Пользователь {Email} успешно вошел в систему", user.Email);

            // Redirect to files page or return URL
            var returnUrl = LoginData.ReturnUrl;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToPage("/Files/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при входе пользователя {Email}", LoginData.Email);
            ErrorMessage = "Произошла ошибка при входе. Попробуйте еще раз.";
            return Page();
        }
    }
}