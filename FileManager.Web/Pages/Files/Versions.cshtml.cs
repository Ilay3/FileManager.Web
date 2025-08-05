using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace FileManager.Web.Pages.Files;

[Authorize]
public class VersionsModel : PageModel
{
    private readonly IFileService _fileService;
    private readonly IFileVersionService _fileVersionService;
    private readonly IFilePreviewService _filePreviewService;

    public VersionsModel(
        IFileService fileService,
        IFileVersionService fileVersionService,
        IFilePreviewService filePreviewService)
    {
        _fileService = fileService;
        _fileVersionService = fileVersionService;
        _filePreviewService = filePreviewService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid FileId { get; set; }

    public List<FileVersionDto> Versions { get; set; } = new();
    public string FileName { get; set; } = string.Empty;
    public string FileIconClass { get; set; } = "bi bi-file-earmark";
    public string FormattedSize { get; set; } = string.Empty;
    public List<string> ActiveEditors { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";

        // Получаем информацию о файле
        var file = await _fileService.GetFileByIdAsync(FileId, userId, isAdmin);
        if (file == null)
        {
            return NotFound();
        }

        FileName = file.Name;
        FileIconClass = file.FileIconClass;
        FormattedSize = file.FormattedSize;

        // Получаем список версий
        Versions = await _fileVersionService.GetFileVersionsAsync(FileId);

        // Получаем информацию об активных редакторах
        var activeSessions = await _filePreviewService.GetActiveEditSessionsAsync(FileId);
        ActiveEditors = activeSessions.Where(s => s.UserId != userId).Select(s => s.UserName).ToList();

        return Page();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
