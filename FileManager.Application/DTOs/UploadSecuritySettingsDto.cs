using System;

namespace FileManager.Application.DTOs;

public class UploadSecuritySettingsDto
{
    public bool EnableAntivirus { get; set; }

    public int UserQuotaMb { get; set; }

    public string[] BlockedExtensions { get; set; } = Array.Empty<string>();
}

