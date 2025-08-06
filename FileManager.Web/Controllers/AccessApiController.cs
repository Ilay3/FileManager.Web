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
[Route("api/access")]
public class AccessApiController : ControllerBase
{
    private readonly IAccessService _accessService;
    private readonly IUserService _userService;
    private readonly ILogger<AccessApiController> _logger;

    public AccessApiController(
        IAccessService accessService,
        IUserService userService,
        ILogger<AccessApiController> logger)
    {
        _accessService = accessService;
        _userService = userService;
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
        var userId = await GetCurrentUserIdAsync();
        if (!userId.HasValue)
        {
            _logger.LogWarning("GrantAccess called without a valid user ID.");
            return Unauthorized("User identifier claim is missing or invalid.");
        }

        await _accessService.GrantAccessAsync(request.FileId, request.FolderId, request.UserId, request.GroupId, request.AccessType, userId.Value, request.Inherit);
        return Ok();
    }

    [HttpPost("bulk-grant")]
    public async Task<IActionResult> BulkGrant([FromBody] BulkGrantRequest request)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!userId.HasValue)
        {
            _logger.LogWarning("BulkGrant called without a valid user ID.");
            return Unauthorized("User identifier claim is missing or invalid.");
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
        var userId = await GetCurrentUserIdAsync();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Revoke called without a valid user ID.");
            return Unauthorized("User identifier claim is missing or invalid.");
        }

        var result = await _accessService.RevokeAccessAsync(ruleId, userId.Value);
        return result ? NoContent() : NotFound();
    }

    private async Task<Guid?> GetCurrentUserIdAsync()
    {
        _logger.LogDebug("User claims: {Claims}", string.Join(", ", User.Claims.Select(c => $"{c.Type}:{c.Value}")));

        foreach (var claimType in new[] { ClaimTypes.NameIdentifier, "sub", "id" })
        {
            var userIdClaim = User.FindFirst(claimType)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                continue;

            if (Guid.TryParse(userIdClaim, out var id))
                return id;

            var user = await _userService.GetUserByEmailAsync(userIdClaim);
            if (user != null)
                return user.Id;
        }

        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user != null)
                return user.Id;
        }

        _logger.LogWarning("User ID claim is missing or invalid.");
        return null;
    }


    public record GrantAccessRequest(Guid? FileId, Guid? FolderId, Guid? UserId, Guid? GroupId, AccessType AccessType, bool Inherit = true);
    public record BulkGrantRequest(List<GrantAccessRequest> Rules);
}
