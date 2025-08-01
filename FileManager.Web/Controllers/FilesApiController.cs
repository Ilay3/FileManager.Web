using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FilesApiController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesApiController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResultDto<FileDto>>> GetFiles([FromQuery] SearchRequestDto request)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";

        var result = await _fileService.GetFilesAsync(request, userId, isAdmin);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FileDto>> GetFile(Guid id)
    {
        var userId = GetCurrentUserId();
        var file = await _fileService.GetFileByIdAsync(id, userId);

        if (file == null)
            return NotFound();

        return Ok(file);
    }

    [HttpGet("search")]
    public async Task<ActionResult<SearchResultDto<FileDto>>> SearchFiles([FromQuery] SearchRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _fileService.SearchFilesAsync(request, userId);
        return Ok(result);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<List<FileDto>>> GetRecentFiles([FromQuery] int count = 10)
    {
        var userId = GetCurrentUserId();
        var files = await _fileService.GetRecentFilesAsync(userId, count);
        return Ok(files);
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<FileDto>>> GetMyFiles([FromQuery] int count = 50)
    {
        var userId = GetCurrentUserId();
        var files = await _fileService.GetMyFilesAsync(userId, count);
        return Ok(files);
    }

    [HttpGet("folder/{folderId}")]
    public async Task<ActionResult<List<FileDto>>> GetFilesByFolder(Guid folderId)
    {
        var userId = GetCurrentUserId();
        var files = await _fileService.GetFilesByFolderAsync(folderId, userId);
        return Ok(files);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}