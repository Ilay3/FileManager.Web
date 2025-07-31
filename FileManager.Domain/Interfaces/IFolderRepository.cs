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
    Task<int> CountAsync();
}
