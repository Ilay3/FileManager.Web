using FileManager.Application.Services;
using FileManager.Domain.Interfaces;
using FileManager.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace FileManager.Web.Pages.Account;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserService _userService;
    private readonly IFilesRepository _filesRepository;

    public ProfileModel(UserService userService, IFilesRepository filesRepository)
    {
        _userService = userService;
        _filesRepository = filesRepository;
    }

    [BindProperty]
    public ProfileViewModel ProfileData { get; set; } = new();

    public int FilesUploaded { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? ImagePath { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return RedirectToPage("/Account/Login");

        ProfileData.Email = user.Email;
        ProfileData.FullName = user.FullName;
        ProfileData.Department = user.Department;
        ImagePath = user.ProfileImagePath;
        FilesUploaded = (await _filesRepository.GetByUserIdAsync(userId)).Count();
        LastLoginAt = user.LastLoginAt;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            return RedirectToPage("/Account/Login");

        if (!ModelState.IsValid)
        {
            FilesUploaded = (await _filesRepository.GetByUserIdAsync(userId)).Count();
            LastLoginAt = user.LastLoginAt;
            ImagePath = user.ProfileImagePath;
            return Page();
        }

        if (!string.IsNullOrEmpty(ProfileData.CurrentPassword) && !string.IsNullOrEmpty(ProfileData.NewPassword))
        {
            await _userService.ChangePasswordAsync(userId, ProfileData.CurrentPassword, ProfileData.NewPassword);
        }

        await _userService.UpdateProfileAsync(userId, ProfileData.Email, ProfileData.FullName, ProfileData.Department, user.ProfileImagePath);
        return RedirectToPage();
    }
}
