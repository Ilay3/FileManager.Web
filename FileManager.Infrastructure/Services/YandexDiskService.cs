using FileManager.Domain.Interfaces;
using FileManager.Domain.Models;
using FileManager.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Net;

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

    public async Task DeleteFileAsync(string filePath, bool permanently = false)
    {
        try
        {
            var url = $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(filePath)}";
            if (permanently)
            {
                url += "&permanently=true";
            }

            var response = await _httpClient.DeleteAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new($"Failed to delete file: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FilePath}", filePath);
            throw;
        }
    }

    public async Task RestoreFromTrashAsync(string filePath)
    {
        try
        {
            var url = $"{_options.ApiBaseUrl}/trash/resources/restore?path={Uri.EscapeDataString(filePath)}";
            var response = await _httpClient.PutAsync(url, null);
            if (!response.IsSuccessStatusCode)
                throw new($"Failed to restore file: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore file {FilePath} from trash", filePath);
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

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.Conflict &&
                    errorContent.Contains("DiskResourceAlreadyExistsError"))
                {
                    _logger.LogDebug("Folder already exists: {FolderPath}", folderPath);
                    return;
                }

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
            var segments = folderPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var currentPath = "";
            foreach (var segment in segments)
            {
                currentPath += $"/{segment}";
                var checkResponse = await _httpClient.GetAsync(
                    $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(currentPath)}");
                if (!checkResponse.IsSuccessStatusCode)
                {
                    await CreateFolderAsync(currentPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure folder exists: {FolderPath}", folderPath);
            throw;
        }
    }

    public async Task<IEnumerable<YandexDiskItem>> GetFolderContentsAsync(string folderPath)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(folderPath)}&limit=1000");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get contents of folder {FolderPath}: {StatusCode}", folderPath, response.StatusCode);
                return Enumerable.Empty<YandexDiskItem>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (!doc.RootElement.TryGetProperty("_embedded", out var embedded) ||
                !embedded.TryGetProperty("items", out var items))
            {
                return Enumerable.Empty<YandexDiskItem>();
            }

            var result = new List<YandexDiskItem>();

            foreach (var item in items.EnumerateArray())
            {
                var type = item.GetProperty("type").GetString();
                var path = item.GetProperty("path").GetString()?.Replace("disk:", "");
                var name = item.GetProperty("name").GetString() ?? string.Empty;
                var size = item.TryGetProperty("size", out var sizeEl) ? sizeEl.GetInt64() : 0;

                if (path == null) continue;

                result.Add(new YandexDiskItem(path, name, type == "dir", size));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get contents for folder {FolderPath}", folderPath);
            return Enumerable.Empty<YandexDiskItem>();
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
