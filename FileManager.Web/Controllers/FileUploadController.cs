using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Application.Services;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/upload")]
public class FileUploadController : ControllerBase
{
    private readonly FileUploadService _fileUploadService;
    private readonly IFolderService _folderService;

    public FileUploadController(FileUploadService fileUploadService, IFolderService folderService)
    {
        _fileUploadService = fileUploadService;
        _folderService = folderService;
    }

    [HttpPost]
    public async Task<ActionResult> UploadFiles(
        [FromForm] List<IFormFile> files,
        [FromForm] Guid folderId,
        [FromForm] string? comment = null)
    {
        var userId = GetCurrentUserId();

        if (files == null || !files.Any())
        {
            return BadRequest(new { error = "Не выбраны файлы для загрузки" });
        }

        if (folderId == Guid.Empty)
        {
            return BadRequest(new { error = "Не выбрана папка назначения" });
        }

        var results = new List<object>();

        foreach (var file in files)
        {
            try
            {
                var result = await _fileUploadService.UploadFileAsync(file, userId, folderId, comment);
                results.Add(new
                {
                    success = true,
                    fileName = file.FileName,
                    fileId = result.Id,
                    message = "Файл успешно загружен"
                });
            }
            catch (Exception ex)
            {
                results.Add(new
                {
                    success = false,
                    fileName = file.FileName,
                    error = ex.Message
                });
            }
        }

        return Ok(new { results });
    }

    [HttpPost("validate")]
    public async Task<ActionResult> ValidateFiles([FromForm] List<IFormFile> files)
    {
        var results = new List<object>();

        foreach (var file in files)
        {
            var validation = await _fileUploadService.ValidateFileAsync(file);
            results.Add(new
            {
                fileName = file.FileName,
                isValid = validation.IsValid,
                errors = validation.Errors,
                warnings = validation.Warnings,
                size = file.Length,
                formattedSize = FormatFileSize(file.Length)
            });
        }

        return Ok(new { results });
    }

    [HttpGet("folders")]
    public async Task<ActionResult> GetAvailableFolders()
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";

        var folders = await _folderService.GetTreeStructureAsync(userId, isAdmin);
        return Ok(folders);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} Б";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} КБ";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} МБ";
        return $"{bytes / (1024 * 1024 * 1024):F1} ГБ";
    }
}