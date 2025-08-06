using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces;

public interface ITrashService
{
    Task<List<TrashItemDto>> GetTrashAsync(Guid userId, bool isAdmin = false);
    Task<bool> RestoreFileAsync(Guid fileId, Guid userId, bool isAdmin = false);
    Task<bool> RestoreFolderAsync(Guid folderId, Guid userId, bool isAdmin = false);
    Task<bool> DeleteFilePermanentAsync(Guid fileId, Guid userId, bool isAdmin = false);
    Task<bool> DeleteFolderPermanentAsync(Guid folderId, Guid userId, bool isAdmin = false);
}
