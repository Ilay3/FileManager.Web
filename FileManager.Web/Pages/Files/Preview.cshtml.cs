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

    public PreviewModel(IFileService fileService, IFilePreviewService filePreviewService)
    {
        _fileService = fileService;
        _filePreviewService = filePreviewService;
    }

    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileIcon { get; set; } = string.Empty;
    public string FormattedSize { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public string PreviewType { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
    public bool HasActiveEditors { get; set; }
    public List<string> ActiveEditors { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = GetCurrentUserId();

        // Получаем информацию о файле
        var file = await _fileService.GetFileByIdAsync(id, userId);
        if (file == null)
        {
            return NotFound();
        }

        FileId = file.Id;
        FileName = file.Name;
        FileIcon = file.FileIcon;
        FormattedSize = file.FormattedSize;
        FileType = file.FileType.ToString();
        CreatedAt = file.CreatedAt;
        UploadedBy = file.UploadedByName;
        Tags = file.Tags;

        // Определяем тип предпросмотра
        PreviewType = DeterminePreviewType(file.Extension);

        // Проверяем возможность редактирования
        CanEdit = await _filePreviewService.CanEditOnlineAsync(file.Extension);

        // Получаем информацию об активных редакторах
        var activeSessions = await _filePreviewService.GetActiveEditSessionsAsync(id);
        var otherEditors = activeSessions.Where(s => s.UserId != userId).ToList();

        HasActiveEditors = otherEditors.Any();
        ActiveEditors = otherEditors.Select(s => s.UserName).ToList();

        return Page();
    }

    private string DeterminePreviewType(string extension)
    {
        var ext = extension.ToLower();

        if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" }.Contains(ext))
            return "image";

        if (ext == ".pdf")
            return "pdf";

        if (ext == ".txt")
            return "text";

        if (new[] { ".docx", ".xlsx", ".pptx" }.Contains(ext))
            return "office";

        return "none";
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}