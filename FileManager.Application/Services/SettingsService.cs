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
}
