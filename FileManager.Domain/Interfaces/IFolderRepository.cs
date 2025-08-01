using FileManager.Domain.Entities;

namespace FileManager.Domain.Interfaces;

public interface IFolderRepository
{
    Task<Folder?> GetByIdAsync(Guid id);
    Task<IEnumerable<Folder>> GetRootFoldersAsync();
    Task<IEnumerable<Folder>> GetSubFoldersAsync(Guid parentId);
    Task<Folder> CreateAsync(Folder folder);
    Task<Folder> UpdateAsync(Folder folder);
    Task DeleteAsync(Guid id);
    Task<Folder?> GetByYandexPathAsync(string yandexPath);
    Task<IEnumerable<Folder>> GetFolderTreeAsync(Guid? rootFolderId = null);
    Task<IEnumerable<Folder>> GetUserAccessibleFoldersAsync(Guid userId);
    Task<int> CountAsync();
    Task<int> GetFilesCountInFolderAsync(Guid folderId);
    Task<int> GetSubFoldersCountAsync(Guid folderId);
}