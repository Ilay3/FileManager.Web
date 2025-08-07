using FileManager.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Configuration;

public class VersioningOptionsAdapter : IVersioningOptions
{
    private readonly VersioningOptions _options;

    public VersioningOptionsAdapter(IOptions<VersioningOptions> options)
    {
        _options = options.Value;
    }

    public bool Enabled => _options.Enabled;
    public int MaxVersionsPerFile => _options.MaxVersionsPerFile;
    public int RetentionDays => _options.RetentionDays;
}
