using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace FileManager.Web.Pages.Files;

[Authorize]
public class UploadModel : PageModel
{
    private readonly IFolderService _folderService;
    private readonly FileUploadService _fileUploadService;

    public UploadModel(IFolderService folderService, FileUploadService fileUploadService)
    {
        _folderService = folderService;
        _fileUploadService = fileUploadService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid FolderId { get; set; }

    [BindProperty]
    public List<IFormFile> Files { get; set; } = new();

    [BindProperty]
    public string? Comment { get; set; }

    public List<TreeNodeDto> Folders { get; set; } = new();

    public async Task OnGet()
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        Folders = await _folderService.GetTreeStructureAsync(userId, isAdmin);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = GetCurrentUserId();
        foreach (var file in Files)
        {
            await _fileUploadService.UploadFileAsync(file, userId, FolderId, Comment);
        }
        return RedirectToPage("Index");
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
