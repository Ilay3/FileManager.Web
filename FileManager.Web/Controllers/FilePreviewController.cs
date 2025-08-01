using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/files")]
public class FilePreviewController : ControllerBase
{
    private readonly IFilePreviewService _filePreviewService;

    public FilePreviewController(IFilePreviewService filePreviewService)
    {
        _filePreviewService = filePreviewService;
    }

    [HttpGet("{id}/preview")]
    public async Task<ActionResult> GetPreview(Guid id)
    {
        var userId = GetCurrentUserId();
        var previewUrl = await _filePreviewService.GetPreviewUrlAsync(id, userId);

        if (previewUrl == null)
        {
            return NotFound("File not found or cannot be previewed");
        }

        // Если это внешняя ссылка (Yandex.Docs), возвращаем её
        if (previewUrl.StartsWith("http"))
        {
            return Ok(new { previewUrl, external = true });
        }

        // Если это внутренняя ссылка, возвращаем её
        return Ok(new { previewUrl, external = false });
    }

    [HttpGet("{id}/edit")]
    public async Task<ActionResult> GetEditUrl(Guid id)
    {
        var userId = GetCurrentUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

        // Проверяем активные сессии редактирования
        var activeSessions = await _filePreviewService.GetActiveEditSessionsAsync(id);
        var otherActiveSessions = activeSessions.Where(s => s.UserId != userId).ToList();

        if (otherActiveSessions.Any())
        {
            var warnings = otherActiveSessions.Select(s =>
                $"{s.UserName} редактирует файл с {s.StartedAt:HH:mm}").ToList();

            return Ok(new
            {
                hasActiveEditors = true,
                warnings,
                canProceed = true // Пользователь может продолжить, но с предупреждением
            });
        }

        var editUrl = await _filePreviewService.GetEditUrlAsync(id, userId, ipAddress, userAgent);

        if (editUrl == null)
        {
            return NotFound("File not found or cannot be edited");
        }

        return Ok(new { editUrl, hasActiveEditors = false });
    }

    [HttpGet("{id}/content")]
    public async Task<ActionResult> GetContent(Guid id)
    {
        var userId = GetCurrentUserId();
        var contentStream = await _filePreviewService.GetFileContentAsync(id, userId);

        if (contentStream == null)
        {
            return NotFound("File not found or access denied");
        }

        // Определяем MIME тип по расширению
        var file = await _filePreviewService.GetFileInfoAsync(id, userId);
        var contentType = GetContentType(file?.Extension ?? "");

        return File(contentStream, contentType);
    }

    [HttpGet("{id}/sessions")]
    public async Task<ActionResult> GetActiveSessions(Guid id)
    {
        var sessions = await _filePreviewService.GetActiveEditSessionsAsync(id);
        return Ok(sessions);
    }

    [HttpPost("sessions/{sessionId}/end")]
    public async Task<ActionResult> EndEditSession(Guid sessionId)
    {
        var userId = GetCurrentUserId();
        await _filePreviewService.EndEditSessionAsync(sessionId, userId);
        return Ok();
    }

    [HttpGet("{id}/info")]
    public async Task<ActionResult> GetFileInfo(Guid id)
    {
        var userId = GetCurrentUserId();
        var canPreview = false;
        var canEdit = false;

        var file = await _filePreviewService.GetFileInfoAsync(id, userId);
        if (file != null)
        {
            canPreview = await _filePreviewService.CanPreviewAsync(file.Extension);
            canEdit = await _filePreviewService.CanEditOnlineAsync(file.Extension);
        }

        var activeSessions = await _filePreviewService.GetActiveEditSessionsAsync(id);

        return Ok(new
        {
            canPreview,
            canEdit,
            activeSessions = activeSessions.Count,
            activeEditors = activeSessions.Where(s => s.UserId != userId).Select(s => s.UserName).ToList()
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }
}