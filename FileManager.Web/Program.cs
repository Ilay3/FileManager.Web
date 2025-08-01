using FileManager.Application.Services;
using FileManager.Application.Interfaces;
using FileManager.Domain.Interfaces;
using FileManager.Infrastructure.Configuration;
using FileManager.Infrastructure.Data;
using FileManager.Infrastructure.Repositories;
using FileManager.Infrastructure.Services;
using FileManager.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Настройка Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрация репозиториев
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFilesRepository, FilesRepository>();
builder.Services.AddScoped<IFolderRepository, FolderRepository>();

// Регистрация сервисов Application layer
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<FilesService>();
builder.Services.AddScoped<StatisticsService>();

// Регистрация сервисов Infrastructure layer
builder.Services.AddScoped<IEmailService, EmailService>();

// Настройка конфигураций
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.Configure<YandexDiskOptions>(
    builder.Configuration.GetSection(YandexDiskOptions.SectionName));
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<AuditOptions>(
    builder.Configuration.GetSection(AuditOptions.SectionName));
builder.Services.Configure<VersioningOptions>(
    builder.Configuration.GetSection(VersioningOptions.SectionName));

// Настройка аутентификации
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "FileManager.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Настройка авторизации
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("IsAdmin", "True"));
});

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Files/Index");
    options.Conventions.AuthorizePage("/Files");
    options.Conventions.AuthorizePage("/Admin", "AdminOnly");
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// Middleware для отслеживания активности пользователей
app.UseUserActivity();

app.MapRazorPages();

// Перенаправление с корня на страницу файлов для авторизованных пользователей
app.MapGet("/", async context =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/Files");
    }
    else
    {
        context.Response.Redirect("/Account/Login");
    }
});

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();

    try
    {
        await context.Database.EnsureCreatedAsync();
        await DatabaseInitializer.InitializeAsync(context, userService);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка инициализации базы данных");
    }
}

app.Run();