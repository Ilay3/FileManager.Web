namespace FileManager.Domain.Models;

public record YandexDiskItem(string Path, string Name, bool IsDirectory, long Size);
