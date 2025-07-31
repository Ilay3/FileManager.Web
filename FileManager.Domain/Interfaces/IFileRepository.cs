using FileManager.Domain.Entities;

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
    Task<int> CountAsync();
    Task<long> GetTotalSizeAsync();
}
