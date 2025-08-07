using System;

namespace FileManager.Application.DTOs;

public class EmailSettingsDto
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public string PasswordResetTemplate { get; set; } = string.Empty;
    public string AccountLockedTemplate { get; set; } = string.Empty;
    public string WelcomeTemplate { get; set; } = string.Empty;
    public string EmailConfirmationTemplate { get; set; } = string.Empty;
    public string TestTemplate { get; set; } = string.Empty;
    public string TestEmail { get; set; } = string.Empty;
}
