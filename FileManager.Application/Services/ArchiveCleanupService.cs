using FileManager.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

namespace FileManager.Application.Services;

public class ArchiveCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ArchiveCleanupService> _logger;
    private readonly ICleanupOptions _options;
    private readonly TimeSpan _interval = TimeSpan.FromDays(1);

    public ArchiveCleanupService(IServiceProvider serviceProvider,
        ICleanupOptions options,
        ILogger<ArchiveCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Archive cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupArchiveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during archive cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Archive cleanup service stopped");
    }

    private async Task CleanupArchiveAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-_options.ArchiveCleanupDays);
        var oldVersions = await context.FileVersions
            .Where(v => v.CreatedAt < cutoff)
            .ToListAsync();

        if (oldVersions.Count == 0)
            return;

        foreach (var version in oldVersions)
        {
            if (version.LocalArchivePath != null && File.Exists(version.LocalArchivePath))
            {
                try
                {
                    File.Delete(version.LocalArchivePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deleting archive file {Path}", version.LocalArchivePath);
                }
            }
        }

        context.FileVersions.RemoveRange(oldVersions);
        await context.SaveChangesAsync();

        _logger.LogInformation("Removed {Count} archived versions older than {Days} days", oldVersions.Count, _options.ArchiveCleanupDays);
    }
}
