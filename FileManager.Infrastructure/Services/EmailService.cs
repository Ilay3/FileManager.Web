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

        var body = $@"
            <h2>Сброс пароля</h2>
            <p>Здравствуйте, {userName}!</p>
            <p>Вы запросили сброс пароля для вашего аккаунта в системе FileManager.</p>
            <p>Для создания нового пароля перейдите по ссылке:</p>
            <p><a href='{resetLink}'>Сбросить пароль</a></p>
            <p>Ссылка будет действительна в течение 1 часа.</p>
            <p>Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо.</p>
            <br>
            <p>С уважением,<br>Команда FileManager</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendAccountLockedEmailAsync(string email, string userName, string reason)
    {
        if (!_emailOptions.Enabled) return;

        var subject = "Ваш аккаунт заблокирован";
        var body = $@"
            <h2>Аккаунт заблокирован</h2>
            <p>Здравствуйте, {userName}!</p>
            <p>Ваш аккаунт в системе FileManager был заблокирован.</p>
            <p><strong>Причина:</strong> {reason}</p>
            <p>Для разблокировки аккаунта обратитесь к администратору системы.</p>
            <br>
            <p>С уважением,<br>Команда FileManager</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string email, string userName, string temporaryPassword)
    {
        if (!_emailOptions.Enabled) return;

        var subject = "Добро пожаловать в FileManager";
        var body = $@"
            <h2>Добро пожаловать в FileManager</h2>
            <p>Здравствуйте, {userName}!</p>
            <p>Для вас был создан аккаунт в системе FileManager.</p>
            <p><strong>Данные для входа:</strong></p>
            <p>Email: {email}<br>
            Временный пароль: {temporaryPassword}</p>
            <p>После первого входа настоятельно рекомендуем сменить пароль.</p>
            <p><a href='https://localhost:7191/Account/Login'>Войти в систему</a></p>
            <br>
            <p>С уважением,<br>Команда FileManager</p>
        ";

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
        var body = $@"
            <h2>Подтверждение регистрации</h2>
            <p>Здравствуйте, {userName}!</p>
            <p>Ваш код подтверждения: <strong>{code}</strong></p>
            <p>Введите его на странице подтверждения email.</p>
            <br>
            <p>С уважением,<br>Команда FileManager</p>
        ";

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