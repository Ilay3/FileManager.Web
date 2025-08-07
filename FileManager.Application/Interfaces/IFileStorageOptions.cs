namespace FileManager.Application.Interfaces;

public interface IFileStorageOptions
{
    string ArchivePath { get; }
    long MaxFileSize { get; }
    string[] AllowedExtensions { get; }
    bool CreateFolderIfNotExists { get; }
    long QuotaPerUser { get; }
}