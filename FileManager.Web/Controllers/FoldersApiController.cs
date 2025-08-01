using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
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
        var contents = await _folderService.GetFolderContentsAsync(folderId, userId, searchRequest);

        if (contents == null)
            return NotFound();

        return Ok(contents);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}