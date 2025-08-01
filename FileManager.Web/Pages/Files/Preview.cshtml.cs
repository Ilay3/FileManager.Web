using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace FileManager.Web.Pages.Files;

[Authorize]
public class PreviewModel : PageModel
{
    private readonly IFileService _fileService;
    private readonly IFilePreviewService _filePreviewService;
    private readonly IFileVersionService _fileVersionService;

    public PreviewModel(
        IFileService fileService,
        IFilePreviewService filePreviewService,
        IFileVersionService fileVersionService)
    {
        _fileService = fileService;
        _filePreviewService = filePreviewService;
        _fileVersionService = fileVersionService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public string FileId => Id.ToString();
    public string FileName { get; set; } = string.Empty;
    public string FileIcon { get; set; } = "📄";
    public string FormattedSize { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public string PreviewType { get; set; } = string.Empty;
    public bool CanPreview { get; set; }
    public bool CanEdit { get; set; }
    public bool HasActiveEditors { get; set; }
    public List<string> ActiveEditors { get; set; } = new();
    public int VersionsCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetCurrentUserId();

        var file = await _fileService.GetFileByIdAsync(Id, userId);
        if (file == null)
        {
            return NotFound();
        }

        FileName = file.Name;
        FileIcon = file.FileIcon;
        FormattedSize = file.FormattedSize;
        FileType = file.FileType.ToString();
        CreatedAt = file.CreatedAt;
        UploadedBy = file.UploadedByName;
        Tags = file.Tags;

        // Определяем тип предпросмотра
        var extension = file.Extension.ToLower();
        if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(extension))
        {
            PreviewType = "image";
        }
        else if (extension == ".pdf")
        {
            PreviewType = "pdf";
        }
        else if (extension == ".txt")
        {
            PreviewType = "text";
        }
        else if (new[] { ".docx", ".xlsx", ".pptx" }.Contains(extension))
        {
            PreviewType = "office";
        }
        else
        {
            PreviewType = "none";
        }

        CanPreview = await _filePreviewService.CanPreviewAsync(file.Extension);
        CanEdit = await _filePreviewService.CanEditOnlineAsync(file.Extension);

        // Получаем информацию об активных редакторах
        var activeSessions = await _filePreviewService.GetActiveEditSessionsAsync(Id);
        ActiveEditors = activeSessions.Where(s => s.UserId != userId).Select(s => s.UserName).ToList();
        HasActiveEditors = ActiveEditors.Any();

        // Получаем количество версий
        var versions = await _fileVersionService.GetFileVersionsAsync(Id);
        VersionsCount = versions.Count;

        return Page();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}