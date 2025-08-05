using FileManager.Application.DTOs;
using System.Collections.Generic;
using System.IO;

namespace FileManager.Application.Interfaces;

public interface IFileService
{
    Task<SearchResultDto<FileDto>> GetFilesAsync(SearchRequestDto request, Guid userId, bool isAdmin = false);
    Task<FileDto?> GetFileByIdAsync(Guid id, Guid userId, bool isAdmin = false);
    Task<List<FileDto>> GetFilesByFolderAsync(Guid folderId, Guid userId, bool isAdmin = false);
    Task<SearchResultDto<FileDto>> SearchFilesAsync(SearchRequestDto request, Guid userId, bool isAdmin = false);
    Task<List<FileDto>> GetRecentFilesAsync(Guid userId, int count = 10);
    Task<List<FileDto>> GetMyFilesAsync(Guid userId, int count = 50);
    Task UpdateTagsAsync(Guid fileId, string tags, Guid userId, bool isAdmin = false);
    Task<Stream> DownloadFilesZipAsync(IEnumerable<Guid> ids, Guid userId, bool isAdmin = false);
    Task DeleteFileAsync(Guid id, Guid userId, bool isAdmin = false);
}
