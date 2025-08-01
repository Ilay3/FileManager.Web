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
        // ���� ������������ ��� �����������, ��������������
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
            // �������� IP ����� ��� �����������
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // ���������� ������������
            var user = await _userService.ValidateUserAsync(LoginData.Email, LoginData.Password);

            if (user == null)
            {
                ErrorMessage = "�������� email ��� ������";
                _logger.LogWarning("��������� ������� ����� ��� {Email} � IP {IP}",
                    LoginData.Email, ipAddress);
                return Page();
            }

            // ���������, �� ������������ �� �������
            if (!user.IsActive)
            {
                ErrorMessage = "��� ������� ������������. ���������� � ��������������.";
                _logger.LogWarning("������� ����� ���������������� ������������ {Email}", LoginData.Email);
                return Page();
            }

            // ������� claims ��� �����������
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

            _logger.LogInformation("�������� ���� ������������ {Email} ({UserId})",
                user.Email, user.Id);

            // �������������� ������������
            if (!string.IsNullOrEmpty(LoginData.ReturnUrl) && Url.IsLocalUrl(LoginData.ReturnUrl))
            {
                return Redirect(LoginData.ReturnUrl);
            }

            return Redirect("/Files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "������ ��� ����� ������������ {Email}", LoginData.Email);
            ErrorMessage = "��������� ������ ��� ����� � �������. ���������� �����.";
            return Page();
        }
    }
}