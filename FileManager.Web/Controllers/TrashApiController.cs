using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/trash")]
public class TrashApiController : ControllerBase
{
    private readonly ITrashService _trashService;

    public TrashApiController(ITrashService trashService)
    {
        _trashService = trashService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTrash()
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        var items = await _trashService.GetTrashAsync(userId, isAdmin);
        return Ok(items);
    }

    [HttpPost("restore/file/{id}")]
    public async Task<IActionResult> RestoreFile(Guid id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        var result = await _trashService.RestoreFileAsync(id, userId, isAdmin);
        return result ? NoContent() : Forbid();
    }

    [HttpPost("restore/folder/{id}")]
    public async Task<IActionResult> RestoreFolder(Guid id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        var result = await _trashService.RestoreFolderAsync(id, userId, isAdmin);
        return result ? NoContent() : Forbid();
    }

    [HttpDelete("file/{id}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        var result = await _trashService.DeleteFilePermanentAsync(id, userId, isAdmin);
        return result ? NoContent() : Forbid();
    }

    [HttpDelete("folder/{id}")]
    public async Task<IActionResult> DeleteFolder(Guid id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        var result = await _trashService.DeleteFolderPermanentAsync(id, userId, isAdmin);
        return result ? NoContent() : Forbid();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
