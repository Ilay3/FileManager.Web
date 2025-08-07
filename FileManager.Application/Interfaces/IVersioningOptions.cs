namespace FileManager.Application.Interfaces;

public interface IVersioningOptions
{
    bool Enabled { get; }
    int MaxVersionsPerFile { get; }
    int RetentionDays { get; }
}
