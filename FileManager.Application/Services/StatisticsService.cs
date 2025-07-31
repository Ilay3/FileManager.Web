using FileManager.Domain.Interfaces;

namespace FileManager.Application.Services;

public class StatisticsService
{
    private readonly IUserRepository _userRepository;
    private readonly IFilesRepository _filesRepository;
    private readonly IFolderRepository _folderRepository;

    public StatisticsService(IUserRepository userRepository, IFilesRepository filesRepository, IFolderRepository folderRepository)
    {
        _userRepository = userRepository;
        _filesRepository = filesRepository;
        _folderRepository = folderRepository;
    }

    public async Task<int> GetUsersCountAsync()
    {
        return await _userRepository.CountAsync();
    }

    public async Task<int> GetFilesCountAsync()
    {
        return await _filesRepository.CountAsync();
    }

    public async Task<int> GetFoldersCountAsync()
    {
        return await _folderRepository.CountAsync();
    }

    public async Task<long> GetTotalFileSizeAsync()
    {
        return await _filesRepository.GetTotalSizeAsync();
    }
}
