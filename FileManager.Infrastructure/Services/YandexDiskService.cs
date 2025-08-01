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

    public YandexDiskService(IOptions<YandexDiskOptions> options, HttpClient httpClient, ILogger<YandexDiskService> logger)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", _options.AccessToken);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderPath)
    {
        try
        {
            // Убираем начальные и конечные слеши
            folderPath = folderPath?.Trim('/') ?? "";

            // Формируем полный путь - НЕ добавляем _options.AppFolderName, он уже в folderPath
            var fullPath = string.IsNullOrEmpty(folderPath)
                ? $"/{_options.AppFolderName}/{fileName}"  // Только если folderPath пустой
                : $"/{folderPath}/{fileName}";  // folderPath уже содержит /FileManager

            _logger.LogDebug("Uploading file to path: {FullPath}", fullPath);

            // Создаем папку если не существует
            var folderToCreate = string.IsNullOrEmpty(folderPath)
                ? $"/{_options.AppFolderName}"
                : $"/{folderPath}";

            await EnsureFolderExistsAsync(folderToCreate);

            // Получаем URL для загрузки с параметром overwrite=true
            var uploadUrlResponse = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources/upload?path={Uri.EscapeDataString(fullPath)}&overwrite=true");

            if (!uploadUrlResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadUrlResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get upload URL. Status: {StatusCode}, Content: {Content}",
                    uploadUrlResponse.StatusCode, errorContent);
                throw new Exception($"Failed to get upload URL: {uploadUrlResponse.StatusCode} - {errorContent}");
            }

            var uploadUrlJson = await uploadUrlResponse.Content.ReadAsStringAsync();
            var uploadData = JsonSerializer.Deserialize<JsonElement>(uploadUrlJson);

            if (!uploadData.TryGetProperty("href", out var hrefElement))
            {
                throw new Exception("Upload URL not found in response");
            }

            var uploadUrl = hrefElement.GetString();
            if (string.IsNullOrEmpty(uploadUrl))
            {
                throw new Exception("Upload URL is empty");
            }

            _logger.LogDebug("Got upload URL: {UploadUrl}", uploadUrl);

            // Загружаем файл
            using var content = new StreamContent(fileStream);
            var uploadResponse = await _httpClient.PutAsync(uploadUrl, content);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to upload file. Status: {StatusCode}, Content: {Content}",
                    uploadResponse.StatusCode, errorContent);
                throw new Exception($"Failed to upload file: {uploadResponse.StatusCode} - {errorContent}");
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
            // Получаем URL для скачивания
            var downloadUrlResponse = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources/download?path={Uri.EscapeDataString(filePath)}");

            if (!downloadUrlResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get download URL: {downloadUrlResponse.StatusCode}");
            }

            var downloadUrlJson = await downloadUrlResponse.Content.ReadAsStringAsync();
            var downloadData = JsonSerializer.Deserialize<JsonElement>(downloadUrlJson);
            var downloadUrl = downloadData.GetProperty("href").GetString();

            // Скачиваем файл
            var fileResponse = await _httpClient.GetAsync(downloadUrl);
            if (!fileResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to download file: {fileResponse.StatusCode}");
            }

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
            {
                throw new Exception($"Failed to delete file: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FilePath}", filePath);
            throw;
        }
    }

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

            // 409 Conflict означает что папка уже существует - это нормально
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create folder. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);
                throw new Exception($"Failed to create folder: {response.StatusCode} - {errorContent}");
            }

            _logger.LogDebug("Folder ensured: {FolderPath}", folderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder {FolderPath}", folderPath);
            throw;
        }
    }

    public async Task<string> GetEditLinkAsync(string filePath)
    {
        try
        {
            // Проверяем, что файл существует
            var fileInfoResponse = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(filePath)}");

            if (!fileInfoResponse.IsSuccessStatusCode)
            {
                throw new Exception($"File not found: {filePath}");
            }

            var fileInfoJson = await fileInfoResponse.Content.ReadAsStringAsync();
            var fileInfo = JsonSerializer.Deserialize<JsonElement>(fileInfoJson);

            // Получаем публичную ссылку для редактирования
            var publishResponse = await _httpClient.PutAsync(
                $"{_options.ApiBaseUrl}/resources/publish?path={Uri.EscapeDataString(filePath)}", null);

            if (!publishResponse.IsSuccessStatusCode)
            {
                // Возможно файл уже опубликован, попробуем получить ссылку
                var metaResponse = await _httpClient.GetAsync(
                    $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(filePath)}");

                if (metaResponse.IsSuccessStatusCode)
                {
                    var metaJson = await metaResponse.Content.ReadAsStringAsync();
                    var metaData = JsonSerializer.Deserialize<JsonElement>(metaJson);

                    if (metaData.TryGetProperty("public_url", out var publicUrlElement))
                    {
                        var publicUrls = publicUrlElement.GetString();
                        return ConvertToEditUrl(publicUrls);
                    }
                }

                throw new Exception($"Failed to get edit link: {publishResponse.StatusCode}");
            }

            // Получаем публичную ссылку
            var updatedInfoResponse = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(filePath)}");

            if (!updatedInfoResponse.IsSuccessStatusCode)
            {
                throw new Exception("Failed to get updated file info");
            }

            var updatedInfoJson = await updatedInfoResponse.Content.ReadAsStringAsync();
            var updatedInfo = JsonSerializer.Deserialize<JsonElement>(updatedInfoJson);

            if (updatedInfo.TryGetProperty("public_url", out var publicUrl))
            {
                var url = publicUrl.GetString();
                return ConvertToEditUrl(url);
            }

            throw new Exception("Failed to get public URL from file info");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get edit link for file {FilePath}", filePath);
            throw;
        }
    }

    private async Task EnsureFolderExistsAsync(string folderPath)
    {
        try
        {
            // Проверяем существование папки
            var checkResponse = await _httpClient.GetAsync(
                $"{_options.ApiBaseUrl}/resources?path={Uri.EscapeDataString(folderPath)}");

            if (checkResponse.IsSuccessStatusCode)
            {
                // Папка уже существует
                return;
            }

            // Создаем папку
            await CreateFolderAsync(folderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure folder exists: {FolderPath}", folderPath);
            throw;
        }
    }

    private string ConvertToEditUrl(string publicUrl)
    {
        // Преобразуем публичную ссылку в ссылку для редактирования
        if (publicUrl.Contains("disk.yandex.ru"))
        {
            var fileId = publicUrl.Split('/').Last();
            return $"https://docs.yandex.ru/docs/view?url=ya-disk-public%3A%2F%2F{fileId}";
        }

        return publicUrl;
    }
}