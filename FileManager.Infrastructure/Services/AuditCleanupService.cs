using FileManager.Application.Interfaces;
using FileManager.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Services;

public class AuditCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromDays(1);
    private readonly int _retentionDays;
    private readonly LogLevel _logLevel;

    public AuditCleanupService(IServiceProvider serviceProvider,
        IOptions<AuditOptions> options,
        ILogger<AuditCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _retentionDays = options.Value.RetentionDays;
        _logLevel = options.Value.LogLevel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Log(_logLevel, "Audit cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldLogsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audit log cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.Log(_logLevel, "Audit cleanup service stopped");
    }

    private async Task CleanupOldLogsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);
        var oldLogs = await context.AuditLogs
            .Where(l => l.CreatedAt < cutoff)
            .ToListAsync();

        if (oldLogs.Count == 0)
            return;

        context.AuditLogs.RemoveRange(oldLogs);
        await context.SaveChangesAsync();

        _logger.Log(_logLevel, "Removed {Count} audit logs older than {Days} days", oldLogs.Count, _retentionDays);
    }
}
