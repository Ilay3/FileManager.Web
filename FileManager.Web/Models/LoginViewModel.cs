using System.ComponentModel.DataAnnotations;

namespace FileManager.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Неверный формат email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; } = false;

    public string? ReturnUrl { get; set; }
}

public class PasswordResetRequestViewModel
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Неверный формат email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

public class PasswordResetViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Новый пароль обязателен")]
    [StringLength(100, ErrorMessage = "Пароль должен содержать минимум {2} символов.", MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Пароль должен содержать минимум 8 символов, включая заглавную букву, цифру и специальный символ")]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтверждение пароля обязательно")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
    [Display(Name = "Подтвердите пароль")]
    public string ConfirmPassword { get; set; } = string.Empty;
}