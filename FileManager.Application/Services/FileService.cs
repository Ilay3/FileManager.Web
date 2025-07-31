using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;

namespace FileManager.Application.Services;

public class FilesService
{
    private readonly IFilesRepository _filesRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly IUserRepository _userRepository;

    public FilesService(IFilesRepository filesRepository, IFolderRepository folderRepository, IUserRepository userRepository)
    {
        _filesRepository = filesRepository;
        _folderRepository = folderRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<Files>> GetUserFilesAsync(Guid userId)
    {
        return await _filesRepository.GetByUserIdAsync(userId);
    }

    public async Task<Files?> GetFileByIdAsync(Guid id)
    {
        return await _filesRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Files>> SearchFilesAsync(string searchTerm)
    {
        return await _filesRepository.SearchByNameAsync(searchTerm);
    }
}
