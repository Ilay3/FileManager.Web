using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Application.Services;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/upload")]
public class FileUploadController : ControllerBase
{
    private readonly FileUploadService _fileUploadService;
    private readonly IFolderService _folderService;
    private readonly ISettingsService _settingsService;
    private readonly VirusScanService _virusScanService;
    private readonly ILogger<FileUploadController> _logger;
    private readonly IAntiforgery _antiforgery;

    public FileUploadController(
        FileUploadService fileUploadService,
        IFolderService folderService,
        ISettingsService settingsService,
        VirusScanService virusScanService,
        ILogger<FileUploadController> logger,
        IAntiforgery antiforgery)
    {
        _fileUploadService = fileUploadService;
        _folderService = folderService;
        _settingsService = settingsService;
        _virusScanService = virusScanService;
        _logger = logger;
        _antiforgery = antiforgery;
    }

    [HttpGet("token")]
    public IActionResult GetToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> UploadFiles(
        [FromForm] List<IFormFile> files,
        [FromForm] Guid folderId,
        [FromForm] string? comment = null)
    {
        var userId = GetCurrentUserId();

        if (files == null || !files.Any())
        {
            var resp = new { results = Array.Empty<object>(), error = "Не выбраны файлы для загрузки" };
            _logger.LogWarning("UploadFiles responded 400: {Response}", resp);
            return BadRequest(resp);
        }

        if (folderId == Guid.Empty)
        {
            var resp = new { results = Array.Empty<object>(), error = "Не выбрана папка назначения" };
            _logger.LogWarning("UploadFiles responded 400: {Response}", resp);
            return BadRequest(resp);
        }

        var results = new List<object>();
        var options = await _settingsService.GetUploadSecurityOptionsAsync();
        var blocked = new HashSet<string>(options.BlockedExtensions.Select(e => e.ToLowerInvariant()));
        var quotaBytes = (long)options.UserQuotaMb * 1024 * 1024;
        var fileMap = new Dictionary<(string FileName, long Size), IFormFile>();
        var duplicates = new List<(string FileName, long Size)>();
        foreach (var file in files)
        {
            var key = (file.FileName, file.Length);
            if (!fileMap.ContainsKey(key))
            {
                fileMap[key] = file;
            }
            else
            {
                duplicates.Add(key);
            }
        }

        if (duplicates.Any())
        {
            var duplicateNames = duplicates
                .Distinct()
                .Select(d => $"{d.FileName} ({FormatFileSize(d.Size)})");
            var resp = new
            {
                results = Array.Empty<object>(),
                error = $"Обнаружены дублирующиеся файлы: {string.Join(", ", duplicateNames)}"
            };
            _logger.LogWarning("UploadFiles detected duplicates: {Duplicates}", duplicateNames);
            return BadRequest(resp);
        }

        foreach (var file in fileMap.Values)
        {
            try
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (blocked.Contains(ext))
                {
                    results.Add(new
                    {
                        success = false,
                        fileName = file.FileName,
                        error = "Данный тип файлов запрещен"
                    });
                    continue;
                }

                if (quotaBytes > 0 && file.Length > quotaBytes)
                {
                    results.Add(new
                    {
                        success = false,
                        fileName = file.FileName,
                        error = "Размер файла превышает квоту пользователя"
                    });
                    continue;
                }

                if (options.EnableAntivirus)
                {
                    using var stream = file.OpenReadStream();
                    var clean = await _virusScanService.ScanAsync(stream);
                    if (!clean)
                    {
                        results.Add(new
                        {
                            success = false,
                            fileName = file.FileName,
                            error = "Файл заражен"
                        });
                        continue;
                    }
                }

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

        var response = new { results };
        _logger.LogInformation("UploadFiles responded 200: {Response}", response);
        return Ok(response);
    }

    [HttpPost("validate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ValidateFiles([FromForm] List<IFormFile> files)
    {
        if (files == null || !files.Any())
        {
            var resp = new { results = Array.Empty<object>(), error = "Не выбраны файлы для проверки" };
            _logger.LogWarning("ValidateFiles responded 400: {Response}", resp);
            return BadRequest(resp);
        }

        try
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

            var response = new { results };
            _logger.LogInformation("ValidateFiles responded 200: {Response}", response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var resp = new { results = Array.Empty<object>(), error = ex.Message };
            _logger.LogError(ex, "ValidateFiles responded 500: {Response}", resp);
            return StatusCode(500, resp);
        }
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