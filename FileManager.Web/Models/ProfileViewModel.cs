using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FileManager.Web.Models;

public class ProfileViewModel
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Department { get; set; }

    public IFormFile? Photo { get; set; }

    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }
}
