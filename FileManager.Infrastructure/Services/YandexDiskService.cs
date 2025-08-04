using FileManager.Domain.Interfaces;
using FileManager.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FileManager.Infrastructure.Services;

public class YandexDiskService : IYandexDiskService
{
    private readonly YandexDiskOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<YandexDiskService> _logger;

    public YandexDiskService(
        IOptions<YandexDiskOptions> options,
        HttpClient httpClient,
        ILogger<YandexDiskService> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", _options.AccessToken);
    }

    #region Upload / Download / Delete

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderPath)
    {
        try
        {
            folderPath = folderPath?.Trim('/') ?? "";

            var fullPath = string.IsNullOrEmpty(folderPath)
                ? $"/{_options.AppFolderName}/{fileName}"
                : $"/{folderPath}/{fileName}";

            _logger.LogDebug("Uploading file to path: {FullPath}", fullPath);

            var folderToCreate = string.IsNullOrEmpty(folderPath)
                ? $"/{_options.AppFolderName}"
                : $"/{folderPath}";

            await EnsureFolderExistsAsync(folderToCreate);

            var uploadUrlResponse = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources/upload?path={Uri.EscapeDataString(fullPath)}&overwrite=true");

            if (!uploadUrlResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadUrlResponse.Content.ReadAsStringAsync();
                throw new($"Failed to get upload URL: {uploadUrlResponse.StatusCode} - {errorContent}");
            }

            var uploadUrl = JsonSerializer.Deserialize<JsonElement>(
                                await uploadUrlResponse.Content.ReadAsStringAsync())
                            .GetProperty("href").GetString();

            using var content = new StreamContent(fileStream);
            var uploadResponse = await _httpClient.PutAsync(uploadUrl!, content);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                throw new($"Failed to upload file: {uploadResponse.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("File uploaded successfully to: {FullPath}", fullPath);
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to {FolderPath}", fileName, folderPath);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string filePath)
    {
        try
        {
            var downloadUrlResponse = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources/download?path={Uri.EscapeDataString(filePath)}");

            if (!downloadUrlResponse.IsSuccessStatusCode)
                throw new($"Failed to get download URL: {downloadUrlResponse.StatusCode}");

            var downloadUrl = JsonSerializer.Deserialize<JsonElement>(
                                  await downloadUrlResponse.Content.ReadAsStringAsync())
                              .GetProperty("href").GetString();

            var fileResponse = await _httpClient.GetAsync(downloadUrl!);

            if (!fileResponse.IsSuccessStatusCode)
                throw new($"Failed to download file: {fileResponse.StatusCode}");

            return await fileResponse.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file {FilePath}", filePath);
            throw;
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(filePath)}");

            if (!response.IsSuccessStatusCode)
                throw new($"Failed to delete file: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FilePath}", filePath);
            throw;
        }
    }

    #endregion

    #region Existence / Folders

    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(filePath)}");

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check file existence {FilePath}", filePath);
            return false;
        }
    }

    public async Task CreateFolderAsync(string folderPath)
    {
        try
        {
            var response = await _httpClient.PutAsync(
                $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(folderPath)}", null);

            if (!response.IsSuccessStatusCode &&
                response.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new($"Failed to create folder: {response.StatusCode} - {errorContent}");
            }

            _logger.LogDebug("Folder ensured: {FolderPath}", folderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder {FolderPath}", folderPath);
            throw;
        }
    }

    private async Task EnsureFolderExistsAsync(string folderPath)
    {
        try
        {
            var checkResponse = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(folderPath)}");

            if (!checkResponse.IsSuccessStatusCode)
                await CreateFolderAsync(folderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure folder exists: {FolderPath}", folderPath);
            throw;
        }
    }

    #endregion

    #region Edit-link

    /// <summary>
    /// Проверяет существование файла и возвращает прямую ссылку на редактор
    /// Yandex Документов. Работает для DOCX / XLSX / PPTX, если пользователь
    /// авторизован тем же аккаунтом, что указан в OAuth-токене.
    /// </summary>
    public async Task<string> GetEditLinkAsync(string filePath)
    {
        try
        {
            var metaResponse = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(filePath)}");

            if (!metaResponse.IsSuccessStatusCode)
                throw new($"File not found: {filePath}");

            return BuildEditorUrl(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get edit link for file {FilePath}", filePath);
            throw;
        }
    }

    private string BuildEditorUrl(string fullPath)
    {
        // fullPath приходит в формате "/Folder/Sub/file.docx"
        var relative = fullPath.TrimStart('/');
        var encoded = Uri.EscapeDataString($"disk/{relative}");
        return $"https://disk.yandex.ru/edit/disk/{encoded}?source=docs";
    }

    #endregion
}
