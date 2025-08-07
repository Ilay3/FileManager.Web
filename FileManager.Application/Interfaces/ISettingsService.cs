using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces;

public interface ISettingsService
{
    Task<StorageSettingsDto> GetStorageOptionsAsync();
    Task SaveStorageOptionsAsync(StorageSettingsDto options);
}
