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
            ErrorMessage = "�������� ������ ������ ������.";
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
                Message = "������ ������� �������! ������ �� ������ ����� � ������� � ����� �������.";
                _logger.LogInformation("������ ������� ������� ��� ������������ {Email}", ResetData.Email);

                // ������� �����
                ResetData = new PasswordResetViewModel();
            }
            else
            {
                ErrorMessage = "������ ��� ������ ������ ��������������� ��� ��������. ��������� ����� ������.";
                _logger.LogWarning("������� ������ ������ � ���������������� ������� ��� {Email}", ResetData.Email);
            }
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogWarning("������ ��������� ������ ��� {Email}: {Error}", ResetData.Email, ex.Message);
        }
        catch (Exception ex)
        {
            ErrorMessage = "��������� ������ ��� ������ ������. ���������� �����.";
            _logger.LogError(ex, "������ ��� ������ ������ ��� {Email}", ResetData.Email);
        }

        return Page();
    }
}