using FileManager.Infrastructure.Data;
using FileManager.Infrastructure.Configuration;
using FileManager.Infrastructure.Repositories;
using FileManager.Application.Services;
using FileManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Настройка PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация конфигураций
builder.Services.Configure<YandexDiskOptions>(
    builder.Configuration.GetSection(YandexDiskOptions.SectionName));

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));

builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));

builder.Services.Configure<AuditOptions>(
    builder.Configuration.GetSection(AuditOptions.SectionName));

builder.Services.Configure<VersioningOptions>(
    builder.Configuration.GetSection(VersioningOptions.SectionName));

builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));

// Настройка аутентификации
var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(securityOptions.SessionTimeoutMinutes);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

// HTTP Client для Яндекс.Диска
builder.Services.AddHttpClient();

// Регистрация репозиториев
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFilesRepository, FilesRepository>();
builder.Services.AddScoped<IFolderRepository, FolderRepository>();

// Регистрация сервисов
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<FilesService>();
builder.Services.AddScoped<StatisticsService>();

// Создание папки архива если не существует
var fileStorageOptions = builder.Configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>() ?? new FileStorageOptions();
if (fileStorageOptions.CreateFolderIfNotExists && !Directory.Exists(fileStorageOptions.ArchivePath))
{
    Directory.CreateDirectory(fileStorageOptions.ArchivePath);
}

var app = builder.Build();

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();

    // Применяем миграции
    context.Database.Migrate();

    // Создаем тестовых пользователей если их нет
    await DatabaseInitializer.InitializeAsync(context, userService);
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

