using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces;

public interface IFileService
{
    Task<SearchResultDto<FileDto>> GetFilesAsync(SearchRequestDto request, Guid userId, bool isAdmin = false);
    Task<FileDto?> GetFileByIdAsync(Guid id, Guid userId);
    Task<List<FileDto>> GetFilesByFolderAsync(Guid folderId, Guid userId);
    Task<SearchResultDto<FileDto>> SearchFilesAsync(SearchRequestDto request, Guid userId);
    Task<List<FileDto>> GetRecentFilesAsync(Guid userId, int count = 10);
    Task<List<FileDto>> GetMyFilesAsync(Guid userId, int count = 50);
}