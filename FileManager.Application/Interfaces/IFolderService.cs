using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces;

public interface IFolderService
{
    Task<List<TreeNodeDto>> GetTreeStructureAsync(Guid userId, bool isAdmin = false);
    Task<FolderDto?> GetFolderByIdAsync(Guid id, Guid userId);
    Task<List<FolderDto>> GetRootFoldersAsync(Guid userId);
    Task<List<FolderDto>> GetSubFoldersAsync(Guid parentId, Guid userId);
    Task<List<BreadcrumbDto>> GetBreadcrumbsAsync(Guid folderId);
    Task<TreeNodeDto?> GetFolderContentsAsync(Guid folderId, Guid userId, SearchRequestDto? searchRequest = null);
}