using FileManager.Application.Interfaces;
using FileManager.Application.Services;
using FileManager.Domain.Interfaces;
using FileManager.Infrastructure.Configuration;
using FileManager.Infrastructure.Data;
using FileManager.Infrastructure.Repositories;
using FileManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileManager.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // База данных
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Настройки конфигурации
        services.Configure<YandexDiskOptions>(configuration.GetSection(YandexDiskOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<AuditOptions>(configuration.GetSection(AuditOptions.SectionName));
        services.Configure<VersioningOptions>(configuration.GetSection(VersioningOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<ThemeOptions>(configuration.GetSection(ThemeOptions.SectionName));
        services.Configure<UploadSecurityOptions>(configuration.GetSection(UploadSecurityOptions.SectionName));
        services.Configure<CleanupOptions>(configuration.GetSection(CleanupOptions.SectionName));

        // Адаптер
        services.AddScoped<IFileStorageOptions, FileStorageOptionsAdapter>();
        services.AddScoped<IVersioningOptions, VersioningOptionsAdapter>();
        services.AddSingleton<ICleanupOptions, CleanupOptionsAdapter>();

        // Репозитории
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFilesRepository, FilesRepository>();
        services.AddScoped<IFolderRepository, FolderRepository>();

        // Infrastructure services
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddHttpClient<IYandexDiskService, YandexDiskService>();

        // Application services
        services.AddScoped<UserService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<FileUploadService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IFolderService, FolderService>();
        services.AddScoped<IUserService, UserDtoService>();
        services.AddScoped<IFilePreviewService, FilePreviewService>();
        services.AddScoped<IFileVersionService, FileVersionService>();
        services.AddScoped<IAccessService, AccessService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<ITrashService, TrashService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<VirusScanService>();

        // Background services
        services.AddHostedService<FileMonitoringService>();
        services.AddHostedService<AuditCleanupService>();
        services.AddHostedService<TrashCleanupService>();
        services.AddHostedService<ArchiveCleanupService>();

        return services;
    }
}