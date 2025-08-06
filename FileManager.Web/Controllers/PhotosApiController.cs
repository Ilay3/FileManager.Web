using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PhotosApiController : ControllerBase
{
    private readonly IFileService _fileService;

    public PhotosApiController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResultDto<FileDto>>> GetPhotos([FromQuery] SearchRequestDto request)
    {
        request.FileType = FileType.Image;
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        var result = await _fileService.SearchFilesAsync(request, userId, isAdmin);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FileDto>> GetPhoto(Guid id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        var file = await _fileService.GetFileByIdAsync(id, userId, isAdmin);
        if (file == null || file.FileType != FileType.Image)
            return NotFound();
        return Ok(file);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePhoto(Guid id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        await _fileService.DeleteFileAsync(id, userId, isAdmin);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
