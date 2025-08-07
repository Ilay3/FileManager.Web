using System;

namespace FileManager.Application.DTOs;

public class SecuritySettingsDto
{
    public bool RequireTwoFactor { get; set; } = false;
    public string PasswordComplexity { get; set; } = "Medium";
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 30;
    public int SessionTimeoutMinutes { get; set; } = 480;
}
