using FileManager.Domain.Entities;
using FileManager.Domain.Enums;

namespace FileManager.Domain.Interfaces;

public interface IFilesRepository
{
    Task<Files?> GetByIdAsync(Guid id);
    Task<IEnumerable<Files>> GetByFolderIdAsync(Guid folderId);
    Task<IEnumerable<Files>> GetByUserIdAsync(Guid userId);
    Task<Files> CreateAsync(Files file);
    Task<Files> UpdateAsync(Files file);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Files>> SearchByNameAsync(string searchTerm);
    Task<IEnumerable<Files>> SearchAsync(string? searchTerm = null, Guid? folderId = null,
        FileType? fileType = null, string? extension = null, DateTime? dateFrom = null,
        DateTime? dateTo = null, Guid? userId = null);
    Task<int> CountAsync();
    Task<long> GetTotalSizeAsync();
    Task<IEnumerable<Files>> GetRecentFilesAsync(Guid userId, int count = 10);
}