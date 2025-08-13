using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FileManager.Application.Services;

public class AccessService : IAccessService
{
    private readonly IAppDbContext _context;
    private readonly IAuditService _audit;
    private readonly ILogger<AccessService> _logger;

    public AccessService(IAppDbContext context, IAuditService audit, ILogger<AccessService> logger)
    {
        _context = context;
        _audit = audit;
        _logger = logger;
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
        if ((fileId.HasValue && folderId.HasValue) || (!fileId.HasValue && !folderId.HasValue))
        {
            _logger.LogWarning("GrantAccessAsync: неверно указаны параметры файла/папки");
            throw new ArgumentException("Должен быть указан либо файл, либо папка");
        }

        if ((userId.HasValue && groupId.HasValue) || (!userId.HasValue && !groupId.HasValue))
        {
            _logger.LogWarning("GrantAccessAsync: неверно указаны получатели");
            throw new ArgumentException("Должен быть указан либо пользователь, либо группа");
        }

        Guid ownerId;
        if (fileId.HasValue)
        {
            ownerId = await _context.Files
                .Where(f => f.Id == fileId.Value)
                .Select(f => f.UploadedById)
                .FirstOrDefaultAsync();
        }
        else
        {
            ownerId = await _context.Folders
                .Where(f => f.Id == folderId!.Value)
                .Select(f => f.CreatedById)
                .FirstOrDefaultAsync();
        }

        if (userId.HasValue && userId.Value == ownerId)
        {
            _logger.LogWarning("GrantAccessAsync: владелец не может быть получателем доступа");
            return;
        }

        if (groupId.HasValue)
        {
            var ownerInGroup = await _context.Groups
                .AnyAsync(g => g.Id == groupId.Value && g.Users.Any(u => u.Id == ownerId));
            if (ownerInGroup)
            {
                _logger.LogWarning("GrantAccessAsync: владелец входит в группу-получатель");
                return;
            }
        }

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

        _logger.LogInformation("Назначены права {AccessType} для {Principal} к {Target}",
            accessType,
            userId ?? groupId,
            fileId ?? folderId);

        await _audit.LogAsync(AuditAction.AccessGranted, grantedById, fileId, folderId,
            $"Granted {accessType} access", isSuccess: true);
    }

    public async Task<bool> RevokeAccessAsync(Guid accessRuleId, Guid revokedById)
    {
        var rule = await _context.AccessRules.FindAsync(accessRuleId);
        if (rule == null)
        {
            _logger.LogWarning("Не найдены права доступа с Id {AccessRuleId}", accessRuleId);
            return false;
        }

        _context.AccessRules.Remove(rule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Отозваны права {AccessType} с Id {AccessRuleId}", rule.AccessType, accessRuleId);

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

        if (file.UploadedById == userId)
            return AccessType.FullAccess;

        var access = await GetDirectAccessAsync(userId, userGroups, fileId, null, false);
        if (access != AccessType.None)
            return access;

        return await GetFolderAccessRecursive(userId, userGroups, file.FolderId);
    }

    public async Task<AccessType> GetEffectiveFolderAccessAsync(Guid userId, Guid folderId)
    {
        var userGroups = await _context.Groups
            .Where(g => g.Users.Any(u => u.Id == userId))
            .Select(g => g.Id)
            .ToListAsync();

        return await GetFolderAccessRecursive(userId, userGroups, folderId);
    }

    private async Task<AccessType> GetFolderAccessRecursive(Guid userId, List<Guid> userGroups, Guid folderId, bool inheritedOnly = false)
    {
        var folder = await _context.Folders
            .Include(f => f.ParentFolder)
            .FirstOrDefaultAsync(f => f.Id == folderId);
        if (folder == null)
            return AccessType.None;

        if (folder.CreatedById == userId)
            return AccessType.FullAccess;

        var access = await GetDirectAccessAsync(userId, userGroups, null, folderId, inheritedOnly);
        if (access != AccessType.None)
            return access;

        if (folder.ParentFolderId.HasValue)
            return await GetFolderAccessRecursive(userId, userGroups, folder.ParentFolderId.Value, true);

        return AccessType.None;
    }

    private async Task<AccessType> GetDirectAccessAsync(Guid userId, List<Guid> userGroups, Guid? fileId, Guid? folderId, bool inheritedOnly)
    {
        var query = _context.AccessRules
            .Where(r => r.FileId == fileId && r.FolderId == folderId &&
                ((r.UserId.HasValue && r.UserId == userId) ||
                 (r.GroupId.HasValue && userGroups.Contains(r.GroupId.Value))));

        if (inheritedOnly)
            query = query.Where(r => r.InheritFromParent);

        var rules = await query.ToListAsync();

        var access = AccessType.None;
        foreach (var rule in rules)
            access |= rule.AccessType;
        return access;
    }
}
