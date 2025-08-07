using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace FileManager.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public SettingsService(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public Task<StorageSettingsDto> GetStorageOptionsAsync()
    {
        var options = new StorageSettingsDto();
        _configuration.GetSection("FileStorage").Bind(options);
        return Task.FromResult(options);
    }

    public async Task SaveStorageOptionsAsync(StorageSettingsDto options)
    {
        var path = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        JsonNode? root = JsonNode.Parse(await File.ReadAllTextAsync(path)) ?? new JsonObject();
        var storage = new JsonObject
        {
            ["ArchivePath"] = options.ArchivePath,
            ["MaxFileSize"] = options.MaxFileSize,
            ["AllowedExtensions"] = new JsonArray(options.AllowedExtensions.Select(e => JsonValue.Create(e)).ToArray()),
            ["CreateFolderIfNotExists"] = options.CreateFolderIfNotExists,
            ["QuotaPerUser"] = options.QuotaPerUser
        };
        root["FileStorage"] = storage;
        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);

        if (_configuration is IConfigurationRoot configurationRoot)
        {
            configurationRoot.Reload();
        }
    }

    public Task<SecuritySettingsDto> GetSecurityOptionsAsync()
    {
        var options = new SecuritySettingsDto();
        _configuration.GetSection("Security").Bind(options);
        return Task.FromResult(options);
    }

    public bool ValidateSecurityOptions(SecuritySettingsDto options)
    {
        var allowed = new[] { "Low", "Medium", "High" };
        if (options.MaxLoginAttempts <= 0) return false;
        if (options.LockoutMinutes <= 0) return false;
        if (options.SessionTimeoutMinutes <= 0) return false;
        if (!allowed.Contains(options.PasswordComplexity)) return false;
        return true;
    }

    public async Task SaveSecurityOptionsAsync(SecuritySettingsDto options)
    {
        if (!ValidateSecurityOptions(options))
        {
            throw new ArgumentException("Invalid security options", nameof(options));
        }

        var path = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        JsonNode? root = JsonNode.Parse(await File.ReadAllTextAsync(path)) ?? new JsonObject();
        var security = new JsonObject
        {
            ["MaxLoginAttempts"] = options.MaxLoginAttempts,
            ["LockoutMinutes"] = options.LockoutMinutes,
            ["SessionTimeoutMinutes"] = options.SessionTimeoutMinutes,
            ["RequireTwoFactor"] = options.RequireTwoFactor,
            ["PasswordComplexity"] = options.PasswordComplexity
        };
        root["Security"] = security;
        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);

        if (_configuration is IConfigurationRoot configurationRoot)
        {
            configurationRoot.Reload();
        }
    }

    public Task<AuditSettingsDto> GetAuditOptionsAsync()
    {
        var options = new AuditSettingsDto();
        _configuration.GetSection("Audit").Bind(options);
        return Task.FromResult(options);
    }

    public async Task SaveAuditOptionsAsync(AuditSettingsDto options)
    {
        var path = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        JsonNode? root = JsonNode.Parse(await File.ReadAllTextAsync(path)) ?? new JsonObject();
        var audit = new JsonObject
        {
            ["EnableFileActions"] = options.EnableFileActions,
            ["EnableUserActions"] = options.EnableUserActions,
            ["EnableAccessLog"] = options.EnableAccessLog,
            ["RetentionDays"] = options.RetentionDays,
            ["LogLevel"] = options.LogLevel
        };
        root["Audit"] = audit;
        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);

        if (_configuration is IConfigurationRoot configurationRoot)
        {
            configurationRoot.Reload();
        }
    }

    public Task<VersioningSettingsDto> GetVersioningOptionsAsync()
    {
        var options = new VersioningSettingsDto();
        _configuration.GetSection("Versioning").Bind(options);
        return Task.FromResult(options);
    }

    public async Task SaveVersioningOptionsAsync(VersioningSettingsDto options)
    {
        var path = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        JsonNode? root = JsonNode.Parse(await File.ReadAllTextAsync(path)) ?? new JsonObject();
        var versioning = new JsonObject
        {
            ["Enabled"] = options.Enabled,
            ["MaxVersionsPerFile"] = options.MaxVersionsPerFile,
            ["RetentionDays"] = options.RetentionDays
        };
        root["Versioning"] = versioning;
        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public Task<EmailSettingsDto> GetEmailOptionsAsync()
    {
        var options = new EmailSettingsDto();
        _configuration.GetSection("Email").Bind(options);
        return Task.FromResult(options);
    }

    public async Task SaveEmailOptionsAsync(EmailSettingsDto options)
    {
        var path = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        JsonNode? root = JsonNode.Parse(await File.ReadAllTextAsync(path)) ?? new JsonObject();
        var email = new JsonObject
        {
            ["SmtpServer"] = options.SmtpServer,
            ["SmtpPort"] = options.SmtpPort,
            ["Username"] = options.Username,
            ["Password"] = options.Password,
            ["FromName"] = options.FromName,
            ["EnableSsl"] = options.EnableSsl,
            ["Enabled"] = options.Enabled,
            ["PasswordResetTemplate"] = options.PasswordResetTemplate,
            ["AccountLockedTemplate"] = options.AccountLockedTemplate,
            ["WelcomeTemplate"] = options.WelcomeTemplate,
            ["EmailConfirmationTemplate"] = options.EmailConfirmationTemplate,
            ["TestTemplate"] = options.TestTemplate,
            ["TestEmail"] = options.TestEmail
        };
        root["Email"] = email;
        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public Task<UploadSecuritySettingsDto> GetUploadSecurityOptionsAsync()
    {
        var options = new UploadSecuritySettingsDto();
        _configuration.GetSection("UploadSecurity").Bind(options);
        return Task.FromResult(options);
    }

    public async Task SaveUploadSecurityOptionsAsync(UploadSecuritySettingsDto options)
    {
        var path = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        JsonNode? root = JsonNode.Parse(await File.ReadAllTextAsync(path)) ?? new JsonObject();
        var upload = new JsonObject
        {
            ["EnableAntivirus"] = options.EnableAntivirus,
            ["UserQuotaMb"] = options.UserQuotaMb,
            ["BlockedExtensions"] = new JsonArray(options.BlockedExtensions.Select(e => JsonValue.Create(e)).ToArray())
        };
        root["UploadSecurity"] = upload;
        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);

        if (_configuration is IConfigurationRoot configurationRoot)
        {
            configurationRoot.Reload();
        }
    }

    public Task<CleanupSettingsDto> GetCleanupOptionsAsync()
    {
        var options = new CleanupSettingsDto();
        _configuration.GetSection("Cleanup").Bind(options);
        return Task.FromResult(options);
    }

    public async Task SaveCleanupOptionsAsync(CleanupSettingsDto options)
    {
        var path = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        JsonNode? root = JsonNode.Parse(await File.ReadAllTextAsync(path)) ?? new JsonObject();
        var cleanup = new JsonObject
        {
            ["TrashRetentionDays"] = options.TrashRetentionDays,
            ["ArchiveCleanupDays"] = options.ArchiveCleanupDays
        };
        root["Cleanup"] = cleanup;
        var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<bool> SendTestEmailAsync(EmailSettingsDto options)
    {
        try
        {
            using var client = new SmtpClient(options.SmtpServer, options.SmtpPort)
            {
                Credentials = new NetworkCredential(options.Username, options.Password),
                EnableSsl = options.EnableSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(options.Username, options.FromName),
                Subject = "Тестовое письмо",
                Body = string.IsNullOrWhiteSpace(options.TestTemplate) ? "Тестовое письмо" : options.TestTemplate,
                IsBodyHtml = true
            };
            message.To.Add(options.TestEmail);

            await client.SendMailAsync(message);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
