using System.ComponentModel.DataAnnotations;

namespace FileManager.Web.Models;

public class EmailConfirmationViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Код подтверждения")]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;
}
