using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccessApiController : ControllerBase
{
    private readonly IAccessService _accessService;
    private readonly ILogger<AccessApiController> _logger;

    public AccessApiController(IAccessService accessService, ILogger<AccessApiController> logger)
    {
        _accessService = accessService;
        _logger = logger;
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
        if (!userId.HasValue)
        {
            _logger.LogWarning("GrantAccess called without a valid user ID.");
            return Unauthorized();
        }

        await _accessService.GrantAccessAsync(request.FileId, request.FolderId, request.UserId, request.GroupId, request.AccessType, userId.Value, request.Inherit);
        return Ok();
    }

    [HttpPost("bulk-grant")]
    public async Task<IActionResult> BulkGrant([FromBody] BulkGrantRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("BulkGrant called without a valid user ID.");
            return Unauthorized();
        }

        foreach (var item in request.Rules)
        {
            await _accessService.GrantAccessAsync(item.FileId, item.FolderId, item.UserId, item.GroupId, item.AccessType, userId.Value, item.Inherit);
        }
        return Ok();
    }

    [HttpDelete("{ruleId}")]
    public async Task<IActionResult> Revoke(Guid ruleId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Revoke called without a valid user ID.");
            return Unauthorized();
        }

        var result = await _accessService.RevokeAccessAsync(ruleId, userId.Value);
        return result ? NoContent() : NotFound();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            _logger.LogWarning("User ID claim is missing.");
            return null;
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid User ID claim: {UserIdClaim}", userIdClaim);
            return null;
        }

        return userId;
    }

    public record GrantAccessRequest(Guid? FileId, Guid? FolderId, Guid? UserId, Guid? GroupId, AccessType AccessType, bool Inherit = true);
    public record BulkGrantRequest(List<GrantAccessRequest> Rules);
}
