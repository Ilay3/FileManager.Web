namespace FileManager.Domain.Interfaces;

public interface IYandexDiskService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderPath);
    Task<Stream> DownloadFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
    Task CreateFolderAsync(string folderPath);
    Task<string> GetEditLinkAsync(string filePath); // для онлайн-редактирования
    Task<IEnumerable<FileManager.Domain.Models.YandexDiskItem>> GetFolderContentsAsync(string folderPath);
}
