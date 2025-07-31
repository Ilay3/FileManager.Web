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

    public LoginModel(UserService userService)
    {
        _userService = userService;
    }

    [BindProperty]
    public LoginViewModel LoginData { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Files/Index");
        }

        LoginData.ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var user = await _userService.ValidateUserAsync(LoginData.Email, LoginData.Password);

            if (user == null)
            {
                ErrorMessage = "Неверный email или пароль";
                return Page();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new("Department", user.Department ?? ""),
                new("IsAdmin", user.IsAdmin.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = LoginData.RememberMe,
                ExpiresUtc = LoginData.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            if (!string.IsNullOrEmpty(LoginData.ReturnUrl) && Url.IsLocalUrl(LoginData.ReturnUrl))
                return Redirect(LoginData.ReturnUrl);

            return RedirectToPage("/Files/Index");
        }
        catch (Exception)
        {
            ErrorMessage = "Ошибка при входе в систему";
            return Page();
        }
    }
}
