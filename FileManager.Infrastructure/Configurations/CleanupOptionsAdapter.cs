using FileManager.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Configuration;

public class CleanupOptionsAdapter : ICleanupOptions
{
    private readonly CleanupOptions _options;

    public CleanupOptionsAdapter(IOptions<CleanupOptions> options)
    {
        _options = options.Value;
    }

    public int TrashRetentionDays => _options.TrashRetentionDays;
    public int ArchiveCleanupDays => _options.ArchiveCleanupDays;
}
