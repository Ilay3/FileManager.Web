using FileManager.Application.Interfaces;
using FileManager.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Configuration;

public class FileStorageOptionsAdapter : IFileStorageOptions
{
    private readonly FileStorageOptions _options;

    public FileStorageOptionsAdapter(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
    }

    public string ArchivePath => _options.ArchivePath;
    public long MaxFileSize => _options.MaxFileSize;
    public string[] AllowedExtensions => _options.AllowedExtensions;
    public bool CreateFolderIfNotExists => _options.CreateFolderIfNotExists;
    public long QuotaPerUser => _options.QuotaPerUser;
}