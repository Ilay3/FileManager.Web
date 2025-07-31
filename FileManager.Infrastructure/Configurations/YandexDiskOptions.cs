namespace FileManager.Infrastructure.Configuration;

public class YandexDiskOptions
{
    public const string SectionName = "YandexDisk";

    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://cloud-api.yandex.net/v1/disk";
    public int UploadTimeout { get; set; } = 300000;
    public long MaxFileSize { get; set; } = 104857600;
    public string AppFolderName { get; set; } = "FileManager";
}
