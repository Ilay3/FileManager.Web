using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Linq;

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
    }
}
