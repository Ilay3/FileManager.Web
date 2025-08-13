using FileManager.Application.DTOs;
using FileManager.Domain.Enums;

namespace FileManager.Application.Interfaces;

public interface IAccessService
{
    Task<List<AccessRuleDto>> GetFileAccessAsync(Guid fileId);
    Task<List<AccessRuleDto>> GetFolderAccessAsync(Guid folderId);
    Task GrantAccessAsync(Guid? fileId, Guid? folderId, Guid? userId, Guid? groupId,
        AccessType accessType, Guid grantedById, bool inherit = true, bool allowOwner = false);
    Task<bool> RevokeAccessAsync(Guid accessRuleId, Guid revokedById);
    Task<AccessType> GetEffectiveAccessAsync(Guid userId, Guid fileId);
    Task<AccessType> GetEffectiveFolderAccessAsync(Guid userId, Guid folderId);
}
