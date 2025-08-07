using System;

namespace FileManager.Application.DTOs;

public class StorageSettingsDto
{
    public string ArchivePath { get; set; } = "./Archive";
    public long MaxFileSize { get; set; } = 104857600;
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public bool CreateFolderIfNotExists { get; set; } = true;
    public long QuotaPerUser { get; set; }
}
