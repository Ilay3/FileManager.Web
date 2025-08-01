using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces;

public interface IFileVersionService
{
    Task<FileVersionDto> CreateVersionAsync(Guid fileId, Guid userId, string comment = null);
    Task<List<FileVersionDto>> GetFileVersionsAsync(Guid fileId);
    Task<FileVersionDto?> GetVersionAsync(Guid versionId);
    Task<Stream?> GetVersionContentAsync(Guid versionId);
    Task<bool> RestoreVersionAsync(Guid fileId, Guid versionId, Guid userId);
    Task CleanupOldVersionsAsync(Guid fileId);
}