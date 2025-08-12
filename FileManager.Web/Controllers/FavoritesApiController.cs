using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/favorites")]
public class FavoritesApiController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesApiController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TreeNodeDto>>> GetFavorites()
    {
        var userId = GetCurrentUserId();
        var items = await _favoriteService.GetFavoritesAsync(userId);
        return Ok(items);
    }

    [HttpPost("files/{fileId}")]
    public async Task<IActionResult> AddFile(Guid fileId)
    {
        var userId = GetCurrentUserId();
        var result = await _favoriteService.AddFileAsync(userId, fileId);
        return result ? Ok() : Forbid();
    }

    [HttpDelete("files/{fileId}")]
    public async Task<IActionResult> RemoveFile(Guid fileId)
    {
        var userId = GetCurrentUserId();
        var result = await _favoriteService.RemoveFileAsync(userId, fileId);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("folders/{folderId}")]
    public async Task<IActionResult> AddFolder(Guid folderId)
    {
        var userId = GetCurrentUserId();
        var result = await _favoriteService.AddFolderAsync(userId, folderId);
        return result ? Ok() : Forbid();
    }

    [HttpDelete("folders/{folderId}")]
    public async Task<IActionResult> RemoveFolder(Guid folderId)
    {
        var userId = GetCurrentUserId();
        var result = await _favoriteService.RemoveFolderAsync(userId, folderId);
        return result ? NoContent() : NotFound();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
