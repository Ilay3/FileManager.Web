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
public class AccessApiController : ControllerBase
{
    private readonly IAccessService _accessService;

    public AccessApiController(IAccessService accessService)
    {
        _accessService = accessService;
    }

    [HttpGet("file/{fileId}")]
    public async Task<ActionResult<List<AccessRuleDto>>> GetFileAccess(Guid fileId)
    {
        var rules = await _accessService.GetFileAccessAsync(fileId);
        return Ok(rules);
    }

    [HttpGet("folder/{folderId}")]
    public async Task<ActionResult<List<AccessRuleDto>>> GetFolderAccess(Guid folderId)
    {
        var rules = await _accessService.GetFolderAccessAsync(folderId);
        return Ok(rules);
    }

    [HttpPost("grant")]
    public async Task<IActionResult> GrantAccess([FromBody] GrantAccessRequest request)
    {
        var userId = GetCurrentUserId();
        await _accessService.GrantAccessAsync(request.FileId, request.FolderId, request.UserId, request.GroupId, request.AccessType, userId, request.Inherit);
        return Ok();
    }

    [HttpPost("bulk-grant")]
    public async Task<IActionResult> BulkGrant([FromBody] BulkGrantRequest request)
    {
        var userId = GetCurrentUserId();
        foreach (var item in request.Rules)
        {
            await _accessService.GrantAccessAsync(item.FileId, item.FolderId, item.UserId, item.GroupId, item.AccessType, userId, item.Inherit);
        }
        return Ok();
    }

    [HttpDelete("{ruleId}")]
    public async Task<IActionResult> Revoke(Guid ruleId)
    {
        var userId = GetCurrentUserId();
        var result = await _accessService.RevokeAccessAsync(ruleId, userId);
        return result ? NoContent() : NotFound();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    public record GrantAccessRequest(Guid? FileId, Guid? FolderId, Guid? UserId, Guid? GroupId, AccessType AccessType, bool Inherit = true);
    public record BulkGrantRequest(List<GrantAccessRequest> Rules);
}
