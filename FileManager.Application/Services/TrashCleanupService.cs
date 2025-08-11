using FileManager.Application.Interfaces;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace FileManager.Application.Services;

public class TrashCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrashCleanupService> _logger;
    private readonly ICleanupOptions _options;
    private readonly TimeSpan _interval = TimeSpan.FromDays(1);

    public TrashCleanupService(IServiceProvider serviceProvider,
        ICleanupOptions options,
        ILogger<TrashCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trash cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupTrashAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during trash cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Trash cleanup service stopped");
    }

    private async Task CleanupTrashAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var filesRepository = scope.ServiceProvider.GetRequiredService<IFilesRepository>();
        var folderRepository = scope.ServiceProvider.GetRequiredService<IFolderRepository>();
        var yandexDiskService = scope.ServiceProvider.GetRequiredService<IYandexDiskService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var cutoff = DateTime.UtcNow.AddDays(-_options.TrashRetentionDays);

        var files = await filesRepository.GetDeletedAsync();
        foreach (var file in files.Where(f => f.DeletedAt.HasValue && f.DeletedAt.Value < cutoff))
        {
            try
            {
                await yandexDiskService.DeleteFileAsync(file.YandexPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting file {Path}", file.YandexPath);
            }

            await filesRepository.HardDeleteAsync(file.Id);
            await auditService.LogAsync(AuditAction.FileDelete, file.UploadedById, fileId: file.Id,
                description: "Файл удалён автоматически из корзины");
        }

        var folders = await folderRepository.GetDeletedAsync();
        foreach (var folder in folders.Where(f => f.DeletedAt.HasValue && f.DeletedAt.Value < cutoff))
        {
            await folderRepository.HardDeleteAsync(folder.Id);
            await auditService.LogAsync(AuditAction.FolderDelete, folder.CreatedById, folderId: folder.Id,
                description: "Папка удалена автоматически из корзины");
        }
    }
}
