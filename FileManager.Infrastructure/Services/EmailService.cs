using FileManager.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using FileManager.Application.Interfaces;

namespace FileManager.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger)
    {
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string userName)
    {
        if (!_emailOptions.Enabled)
        {
            _logger.LogWarning("Email отправка отключена в настройках");
            return;
        }

        var subject = "Сброс пароля FileManager";
        var resetLink = $"https://localhost:7191/Account/ResetPassword?token={resetToken}&email={Uri.EscapeDataString(email)}";

        var body = _emailOptions.PasswordResetTemplate
            .Replace("{UserName}", userName)
            .Replace("{ResetLink}", resetLink);

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendAccountLockedEmailAsync(string email, string userName, string reason)
    {
        if (!_emailOptions.Enabled) return;

        var subject = "Ваш аккаунт заблокирован";
        var body = _emailOptions.AccountLockedTemplate
            .Replace("{UserName}", userName)
            .Replace("{Reason}", reason);

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string userName, string temporaryPassword)
    {
        if (!_emailOptions.Enabled) return;

        var subject = "Добро пожаловать в FileManager";
        var body = _emailOptions.WelcomeTemplate
            .Replace("{UserName}", userName)
            .Replace("{Email}", email)
            .Replace("{Password}", temporaryPassword);

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendEmailConfirmationAsync(string email, string userName, string code)
    {
        if (!_emailOptions.Enabled)
        {
            _logger.LogWarning("Email отправка отключена в настройках");
            return;
        }

        var subject = "Подтверждение регистрации FileManager";
        var body = _emailOptions.EmailConfirmationTemplate
            .Replace("{UserName}", userName)
            .Replace("{Code}", code);

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            using var client = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailOptions.Username, _emailOptions.Password),
                EnableSsl = _emailOptions.EnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailOptions.Username, _emailOptions.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email успешно отправлен на {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки email на {Email}", toEmail);
            throw;
        }
    }
}