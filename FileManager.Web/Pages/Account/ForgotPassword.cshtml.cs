using FileManager.Application.Interfaces;
using FileManager.Application.Services;
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
        if (User.Identity?.IsAuthenticated == true)
        {
            Response.Redirect("/Files");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var token = await _userService.GeneratePasswordResetTokenAsync(ResetRequest.Email);

            if (token != null)
            {
                var user = await _userService.GetUserByEmailAsync(ResetRequest.Email);
                if (user != null)
                {
                    await _emailService.SendPasswordResetEmailAsync(ResetRequest.Email, token, user.FullName);
                    _logger.LogInformation("����� ������ ������ ��������� ��� {Email}", ResetRequest.Email);
                }
            }

            // ������ ���������� �������� ��������� ��� ������������
            Message = "���� ������� � ��������� email ����������, �� ���� ����� ���������� ������ ��� ������ ������.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "������ ��� ������� ������ ������ ��� {Email}", ResetRequest.Email);
            ErrorMessage = "��������� ������ ��� �������� ������. ���������� �����.";
            return Page();
        }
    }
}