using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Application.Services;

public class AccessService : IAccessService
{
    private readonly IAppDbContext _context;
    private readonly IAuditService _audit;

    public AccessService(IAppDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<List<AccessRuleDto>> GetFileAccessAsync(Guid fileId)
    {
        return await _context.AccessRules
            .Where(r => r.FileId == fileId)
            .Include(r => r.User)
            .Include(r => r.Group)
            .Select(r => new AccessRuleDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User != null ? r.User.FullName : null,
                GroupId = r.GroupId,
                GroupName = r.Group != null ? r.Group.Name : null,
                AccessType = r.AccessType,
                InheritFromParent = r.InheritFromParent
            })
            .ToListAsync();
    }

    public async Task<List<AccessRuleDto>> GetFolderAccessAsync(Guid folderId)
    {
        return await _context.AccessRules
            .Where(r => r.FolderId == folderId)
            .Include(r => r.User)
            .Include(r => r.Group)
            .Select(r => new AccessRuleDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User != null ? r.User.FullName : null,
                GroupId = r.GroupId,
                GroupName = r.Group != null ? r.Group.Name : null,
                AccessType = r.AccessType,
                InheritFromParent = r.InheritFromParent
            })
            .ToListAsync();
    }

    public async Task GrantAccessAsync(Guid? fileId, Guid? folderId, Guid? userId, Guid? groupId,
        AccessType accessType, Guid grantedById, bool inherit = true)
    {
        var rule = new AccessRule
        {
            FileId = fileId,
            FolderId = folderId,
            UserId = userId,
            GroupId = groupId,
            AccessType = accessType,
            GrantedById = grantedById,
            InheritFromParent = inherit
        };
        _context.AccessRules.Add(rule);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.AccessGranted, grantedById, fileId, folderId,
            $"Granted {accessType} access", isSuccess: true);
    }

    public async Task<bool> RevokeAccessAsync(Guid accessRuleId, Guid revokedById)
    {
        var rule = await _context.AccessRules.FindAsync(accessRuleId);
        if (rule == null)
            return false;

        _context.AccessRules.Remove(rule);
        await _context.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.AccessRevoked, revokedById, rule.FileId, rule.FolderId,
            $"Revoked {rule.AccessType} access", isSuccess: true);
        return true;
    }

    public async Task<AccessType> GetEffectiveAccessAsync(Guid userId, Guid fileId)
    {
        var userGroups = await _context.Groups
            .Where(g => g.Users.Any(u => u.Id == userId))
            .Select(g => g.Id)
            .ToListAsync();

        var file = await _context.Files
            .Include(f => f.Folder)
            .FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null)
            return AccessType.None;

        var access = await GetDirectAccessAsync(userId, userGroups, fileId, null);
        if (access != AccessType.None)
            return access;

        return await GetFolderAccessRecursive(userId, userGroups, file.FolderId);
    }

    private async Task<AccessType> GetFolderAccessRecursive(Guid userId, List<Guid> userGroups, Guid folderId)
    {
        var folder = await _context.Folders
            .Include(f => f.ParentFolder)
            .FirstOrDefaultAsync(f => f.Id == folderId);
        if (folder == null)
            return AccessType.None;

        var access = await GetDirectAccessAsync(userId, userGroups, null, folderId);
        if (access != AccessType.None)
            return access;

        if (folder.ParentFolderId.HasValue)
            return await GetFolderAccessRecursive(userId, userGroups, folder.ParentFolderId.Value);

        return AccessType.None;
    }

    private async Task<AccessType> GetDirectAccessAsync(Guid userId, List<Guid> userGroups, Guid? fileId, Guid? folderId)
    {
        var rules = await _context.AccessRules
            .Where(r => r.FileId == fileId && r.FolderId == folderId &&
                ((r.UserId.HasValue && r.UserId == userId) ||
                 (r.GroupId.HasValue && userGroups.Contains(r.GroupId.Value))) &&
                r.InheritFromParent)
            .ToListAsync();

        var access = AccessType.None;
        foreach (var rule in rules)
            access |= rule.AccessType;
        return access;
    }
}
