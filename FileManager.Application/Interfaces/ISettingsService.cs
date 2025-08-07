using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces;

public interface ISettingsService
{
    Task<StorageSettingsDto> GetStorageOptionsAsync();
    Task SaveStorageOptionsAsync(StorageSettingsDto options);
    Task<SecuritySettingsDto> GetSecurityOptionsAsync();
    Task SaveSecurityOptionsAsync(SecuritySettingsDto options);
    bool ValidateSecurityOptions(SecuritySettingsDto options);
}
