namespace FileManager.Infrastructure.Configuration;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public string PasswordResetTemplate { get; set; } = "<h2>Сброс пароля</h2><p>Здравствуйте, {UserName}!</p><p>Для создания нового пароля перейдите по ссылке: <a href='{ResetLink}'>Сбросить пароль</a></p>";
    public string AccountLockedTemplate { get; set; } = "<h2>Аккаунт заблокирован</h2><p>Здравствуйте, {UserName}!</p><p>Причина: {Reason}</p>";
    public string WelcomeTemplate { get; set; } = "<h2>Добро пожаловать в FileManager</h2><p>Здравствуйте, {UserName}!</p><p>Email: {Email}<br/>Пароль: {Password}</p>";
    public string EmailConfirmationTemplate { get; set; } = "<h2>Подтверждение регистрации</h2><p>Здравствуйте, {UserName}!</p><p>Ваш код: {Code}</p>";
    public string TestTemplate { get; set; } = "<p>Тестовое письмо FileManager</p>";
    public string TestEmail { get; set; } = string.Empty;
}
