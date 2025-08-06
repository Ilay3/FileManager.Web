using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/folders")]
public class FoldersApiController : ControllerBase
{
    private readonly IFolderService _folderService;

    public FoldersApiController(IFolderService folderService)
    {
        _folderService = folderService;
    }

    [HttpGet("tree")]
    public async Task<ActionResult<List<TreeNodeDto>>> GetTreeStructure()
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";

        var tree = await _folderService.GetTreeStructureAsync(userId, isAdmin);
        return Ok(tree);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FolderDto>> GetFolder(Guid id)
    {
        var userId = GetCurrentUserId();
        var folder = await _folderService.GetFolderByIdAsync(id, userId);

        if (folder == null)
            return NotFound();

        return Ok(folder);
    }

    [HttpGet("root")]
    public async Task<ActionResult<List<FolderDto>>> GetRootFolders()
    {
        var userId = GetCurrentUserId();
        var folders = await _folderService.GetRootFoldersAsync(userId);
        return Ok(folders);
    }

    [HttpGet("{parentId}/subfolders")]
    public async Task<ActionResult<List<FolderDto>>> GetSubFolders(Guid parentId)
    {
        var userId = GetCurrentUserId();
        var folders = await _folderService.GetSubFoldersAsync(parentId, userId);
        return Ok(folders);
    }

    [HttpGet("{folderId}/breadcrumbs")]
    public async Task<ActionResult<List<BreadcrumbDto>>> GetBreadcrumbs(Guid folderId)
    {
        var breadcrumbs = await _folderService.GetBreadcrumbsAsync(folderId);
        return Ok(breadcrumbs);
    }

    [HttpGet("{folderId}/contents")]
    public async Task<ActionResult<TreeNodeDto>> GetFolderContents(Guid folderId, [FromQuery] SearchRequestDto? searchRequest)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        var contents = await _folderService.GetFolderContentsAsync(folderId, userId, searchRequest, isAdmin);

        if (contents == null)
            return NotFound();

        return Ok(contents);
    }

    [HttpPost]
    public async Task<ActionResult<FolderDto>> CreateFolder([FromBody] CreateFolderRequest request)
    {
        var userId = GetCurrentUserId();
        try
        {
            var parentId = request.ParentId == Guid.Empty ? null : request.ParentId;
            var folder = await _folderService.CreateFolderAsync(request.Name, parentId, userId);
            return Ok(folder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/rename")]
    public async Task<ActionResult<FolderDto>> RenameFolder(Guid id, [FromBody] RenameFolderRequest request)
    {
        var userId = GetCurrentUserId();
        try
        {
            var folder = await _folderService.RenameFolderAsync(id, request.Name, userId);
            return folder == null ? NotFound() : Ok(folder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFolder(Guid id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        var result = await _folderService.DeleteFolderAsync(id, userId, isAdmin);
        if (!result)
            return BadRequest("Невозможно удалить папку");
        return NoContent();
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> MoveFolder(Guid id, [FromBody] MoveFolderRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _folderService.MoveFolderAsync(id, request.NewParentId, userId);
        if (!result)
            return BadRequest("Не удалось переместить папку");
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    public record CreateFolderRequest(string Name, Guid? ParentId);
    public record RenameFolderRequest(string Name);
    public record MoveFolderRequest(Guid? NewParentId);
}
