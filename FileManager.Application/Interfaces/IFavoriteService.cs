using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces;

public interface IFavoriteService
{
    Task<bool> AddFileAsync(Guid userId, Guid fileId);
    Task<bool> AddFolderAsync(Guid userId, Guid folderId);
    Task<bool> RemoveFileAsync(Guid userId, Guid fileId);
    Task<bool> RemoveFolderAsync(Guid userId, Guid folderId);
    Task<List<TreeNodeDto>> GetFavoritesAsync(Guid userId);
}
