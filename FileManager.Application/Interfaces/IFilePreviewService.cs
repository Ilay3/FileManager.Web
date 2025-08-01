using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces
{
    public interface IFilePreviewService
    {
        Task<string?> GetPreviewUrlAsync(Guid fileId, Guid userId);
        Task<string?> GetEditUrlAsync(Guid fileId, Guid userId, string? ipAddress = null, string? userAgent = null);
        Task<Stream?> GetFileContentAsync(Guid fileId, Guid userId);
        Task<FileInfoDto?> GetFileInfoAsync(Guid fileId, Guid userId);
        Task<bool> CanPreviewAsync(string extension);
        Task<bool> CanEditOnlineAsync(string extension);
        Task<List<FileEditSessionDto>> GetActiveEditSessionsAsync(Guid fileId);
        Task EndEditSessionAsync(Guid sessionId, Guid userId);
    }

}
