using FileManager.Application.Interfaces;
using FileManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace FileManager.Application.Services;

public class FileMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2); // Проверяем каждые 2 минуты

    public FileMonitoringService(IServiceProvider serviceProvider, ILogger<FileMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("File monitoring service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForFileChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in file monitoring service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("File monitoring service stopped");
    }

    private async Task CheckForFileChanges()
    {
        using var scope = _serviceProvider.CreateScope();
        var filesRepository = scope.ServiceProvider.GetRequiredService<IFilesRepository>();
        var yandexDiskService = scope.ServiceProvider.GetRequiredService<IYandexDiskService>();
        var fileVersionService = scope.ServiceProvider.GetRequiredService<IFileVersionService>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        // Получаем все активные сессии редактирования
        var activeSessions = await context.FileEditSessions
            .Where(s => s.EndedAt == null && s.StartedAt > DateTime.UtcNow.AddHours(-2)) // Активные за последние 2 часа
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            try
            {
                await CheckFileForChanges(session.FileId, session.UserId, filesRepository, yandexDiskService, fileVersionService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file {FileId} for changes", session.FileId);
            }
        }
    }

    private async Task CheckFileForChanges(Guid fileId, Guid userId, IFilesRepository filesRepository,
        IYandexDiskService yandexDiskService, IFileVersionService fileVersionService)
    {
        var file = await filesRepository.GetByIdAsync(fileId);
        if (file == null) return;

        try
        {
            // Получаем информацию о файле из Яндекс.Диска
            var fileModified = await GetFileModificationTime(file.YandexPath, yandexDiskService);

            if (fileModified.HasValue && fileModified.Value > file.UpdatedAt)
            {
                _logger.LogInformation("Detected changes in file {FileId} ({FileName}), creating new version",
                    fileId, file.Name);

                // Создаем новую версию
                await fileVersionService.CreateVersionAsync(fileId, userId, "Автоматическая версия после редактирования в Яндекс.Документах");

                // Обновляем время изменения файла
                file.UpdatedAt = fileModified.Value;
                await filesRepository.UpdateAsync(file);

                _logger.LogInformation("Created new version for file {FileId} due to external changes", fileId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking modifications for file {FileId}", fileId);
        }
    }

    private async Task<DateTime?> GetFileModificationTime(string filePath, IYandexDiskService yandexDiskService)
    {
        try
        {
            // Используем рефлексию чтобы получить HttpClient из YandexDiskService
            var httpClientField = yandexDiskService.GetType().GetField("_httpClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (httpClientField?.GetValue(yandexDiskService) is HttpClient httpClient)
            {
                var response = await httpClient.GetAsync($"https://cloud-api.yandex.net/v1/disk/resources?path={Uri.EscapeDataString(filePath)}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);

                    if (jsonDoc.RootElement.TryGetProperty("modified", out var modifiedElement))
                    {
                        var modifiedString = modifiedElement.GetString();
                        if (DateTime.TryParse(modifiedString, out var modifiedDate))
                        {
                            return modifiedDate.ToUniversalTime();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get modification time for file {FilePath}", filePath);
        }

        return null;
    }
}