namespace FileManager.Infrastructure.Configuration;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string ArchivePath { get; set; } = "./Archive";
    public long MaxFileSize { get; set; } = 104857600;
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public bool CreateFolderIfNotExists { get; set; } = true;
}
