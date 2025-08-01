using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/files/{fileId}/versions")]
public class FileVersionsController : ControllerBase
{
    private readonly IFileVersionService _fileVersionService;

    public FileVersionsController(IFileVersionService fileVersionService)
    {
        _fileVersionService = fileVersionService;
    }

    [HttpGet]
    public async Task<ActionResult> GetVersions(Guid fileId)
    {
        var versions = await _fileVersionService.GetFileVersionsAsync(fileId);
        return Ok(versions);
    }

    [HttpGet("{versionId}")]
    public async Task<ActionResult> GetVersion(Guid fileId, Guid versionId)
    {
        var version = await _fileVersionService.GetVersionAsync(versionId);

        if (version == null || version.FileId != fileId)
            return NotFound();

        return Ok(version);
    }

    [HttpGet("{versionId}/content")]
    public async Task<ActionResult> GetVersionContent(Guid fileId, Guid versionId)
    {
        var version = await _fileVersionService.GetVersionAsync(versionId);

        if (version == null || version.FileId != fileId)
            return NotFound();

        var contentStream = await _fileVersionService.GetVersionContentAsync(versionId);

        if (contentStream == null)
            return NotFound();

        var contentType = GetContentType(Path.GetExtension(version.LocalArchivePath ?? ""));
        var fileName = $"version_{version.VersionNumber}_{Path.GetFileName(version.LocalArchivePath ?? "")}";

        return File(contentStream, contentType, fileName);
    }

    [HttpPost("{versionId}/restore")]
    public async Task<ActionResult> RestoreVersion(Guid fileId, Guid versionId)
    {
        var userId = GetCurrentUserId();
        var success = await _fileVersionService.RestoreVersionAsync(fileId, versionId, userId);

        if (!success)
            return BadRequest("Не удалось восстановить версию файла");

        return Ok(new { message = "Версия файла успешно восстановлена" });
    }

    [HttpPost]
    public async Task<ActionResult> CreateVersion(Guid fileId, [FromBody] CreateVersionRequest request)
    {
        var userId = GetCurrentUserId();

        try
        {
            var version = await _fileVersionService.CreateVersionAsync(fileId, userId, request.Comment);
            return Ok(version);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
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

    public class CreateVersionRequest
    {
        public string? Comment { get; set; }
    }
}