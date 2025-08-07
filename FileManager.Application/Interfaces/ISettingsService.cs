using FileManager.Application.DTOs;

namespace FileManager.Application.Interfaces;

public interface ISettingsService
{
    Task<StorageSettingsDto> GetStorageOptionsAsync();
    Task SaveStorageOptionsAsync(StorageSettingsDto options);
    Task<SecuritySettingsDto> GetSecurityOptionsAsync();
    Task SaveSecurityOptionsAsync(SecuritySettingsDto options);
    bool ValidateSecurityOptions(SecuritySettingsDto options);
    Task<EmailSettingsDto> GetEmailOptionsAsync();
    Task SaveEmailOptionsAsync(EmailSettingsDto options);
    Task<bool> SendTestEmailAsync(EmailSettingsDto options);
    Task<AuditSettingsDto> GetAuditOptionsAsync();
    Task SaveAuditOptionsAsync(AuditSettingsDto options);
    Task<VersioningSettingsDto> GetVersioningOptionsAsync();
    Task SaveVersioningOptionsAsync(VersioningSettingsDto options);
    Task<ThemeSettingsDto> GetThemeOptionsAsync();
    Task SaveThemeOptionsAsync(ThemeSettingsDto options);
    Task<UploadSecuritySettingsDto> GetUploadSecurityOptionsAsync();
    Task SaveUploadSecurityOptionsAsync(UploadSecuritySettingsDto options);
    Task<CleanupSettingsDto> GetCleanupOptionsAsync();
    Task SaveCleanupOptionsAsync(CleanupSettingsDto options);
}
