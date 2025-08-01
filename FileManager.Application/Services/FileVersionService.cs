using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using FileManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FileManager.Infrastructure.Configuration;
using System;

namespace FileManager.Application.Services;

public class FileVersionService : IFileVersionService
{
    private readonly AppDbContext _context;
    private readonly IFilesRepository _filesRepository;
    private readonly IYandexDiskService _yandexDiskService;
    private readonly IAuditService _auditService;
    private readonly IFileStorageOptions _storageOptions;
    private readonly ILogger<FileVersionService> _logger;

    public FileVersionService(
        AppDbContext context,
        IFilesRepository filesRepository,
        IYandexDiskService yandexDiskService,
        IAuditService auditService,
        IFileStorageOptions storageOptions,
        ILogger<FileVersionService> logger)
    {
        _context = context;
        _filesRepository = filesRepository;
        _yandexDiskService = yandexDiskService;
        _auditService = auditService;
        _storageOptions = storageOptions;
        _logger = logger;
    }

    public async Task<FileVersionDto> CreateVersionAsync(Guid fileId, Guid userId, string comment = null)
    {
        var file = await _filesRepository.GetByIdAsync(fileId);
        if (file == null)
            throw new InvalidOperationException("Файл не найден");

        try
        {
            // Получаем текущий номер версии
            var lastVersion = await _context.FileVersions
                .Where(v => v.FileId == fileId)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();

            var nextVersionNumber = (lastVersion?.VersionNumber ?? 0) + 1;

            // Скачиваем файл из Яндекс.Диска
            using var fileStream = await _yandexDiskService.DownloadFileAsync(file.YandexPath);

            // Создаем папку для архива если не существует
            var archiveFolder = Path.Combine(_storageOptions.ArchivePath, fileId.ToString());
            if (!Directory.Exists(archiveFolder))
            {
                Directory.CreateDirectory(archiveFolder);
            }

            // Генерируем имя архивного файла
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            var user = await _context.Users.FindAsync(userId);
            var archiveFileName = $"{timestamp}_{user?.FullName?.Replace(" ", "_") ?? "unknown"}{file.Extension}";
            var archivePath = Path.Combine(archiveFolder, archiveFileName);

            // Сохраняем файл в архив
            using var archiveFileStream = new FileStream(archivePath, FileMode.Create);
            await fileStream.CopyToAsync(archiveFileStream);

            var fileInfo = new FileInfo(archivePath);

            // Создаем запись о версии
            var fileVersion = new FileVersion
            {
                FileId = fileId,
                VersionNumber = nextVersionNumber,
                LocalArchivePath = archivePath,
                SizeBytes = fileInfo.Length,
                Comment = comment ?? $"Автоматическая версия после редактирования",
                CreatedById = userId,
                IsCurrentVersion = true
            };

            _context.FileVersions.Add(fileVersion);

            // Обновляем предыдущие версии
            var previousVersions = await _context.FileVersions
                .Where(v => v.FileId == fileId && v.Id != fileVersion.Id)
                .ToListAsync();

            foreach (var prev in previousVersions)
            {
                prev.IsCurrentVersion = false;
            }

            // Обновляем ссылку на текущую версию в файле
            file.CurrentVersionId = fileVersion.Id;
            file.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Логируем создание версии
            await _auditService.LogAsync(
                AuditAction.FileEdit,
                userId,
                fileId,
                description: $"Создана версия {nextVersionNumber} файла {file.Name}",
                isSuccess: true);

            _logger.LogInformation("Created version {VersionNumber} for file {FileId} by user {UserId}",
                nextVersionNumber, fileId, userId);

            // Очищаем старые версии
            await CleanupOldVersionsAsync(fileId);

            return MapToDto(fileVersion, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating version for file {FileId}", fileId);

            await _auditService.LogAsync(
                AuditAction.FileEdit,
                userId,
                fileId,
                description: $"Ошибка создания версии файла {file.Name}",
                isSuccess: false,
                errorMessage: ex.Message);

            throw;
        }
    }

    public async Task<List<FileVersionDto>> GetFileVersionsAsync(Guid fileId)
    {
        var versions = await _context.FileVersions
            .Include(v => v.CreatedBy)
            .Where(v => v.FileId == fileId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();

        return versions.Select(v => MapToDto(v, v.CreatedBy)).ToList();
    }

    public async Task<FileVersionDto?> GetVersionAsync(Guid versionId)
    {
        var version = await _context.FileVersions
            .Include(v => v.CreatedBy)
            .FirstOrDefaultAsync(v => v.Id == versionId);

        return version == null ? null : MapToDto(version, version.CreatedBy);
    }

    public async Task<Stream?> GetVersionContentAsync(Guid versionId)
    {
        var version = await _context.FileVersions.FindAsync(versionId);
        if (version?.LocalArchivePath == null || !File.Exists(version.LocalArchivePath))
            return null;

        return new FileStream(version.LocalArchivePath, FileMode.Open, FileAccess.Read);
    }

    public async Task<bool> RestoreVersionAsync(Guid fileId, Guid versionId, Guid userId)
    {
        var file = await _filesRepository.GetByIdAsync(fileId);
        var version = await _context.FileVersions.FindAsync(versionId);

        if (file == null || version == null || version.FileId != fileId)
            return false;

        try
        {
            // Создаем новую версию из текущего состояния перед восстановлением
            await CreateVersionAsync(fileId, userId, "Версия перед восстановлением");

            // Загружаем архивную версию обратно в Яндекс.Диск
            if (version.LocalArchivePath != null && File.Exists(version.LocalArchivePath))
            {
                using var archiveStream = new FileStream(version.LocalArchivePath, FileMode.Open, FileAccess.Read);
                await _yandexDiskService.UploadFileAsync(archiveStream, Path.GetFileName(file.YandexPath),
                    Path.GetDirectoryName(file.YandexPath)?.Replace("\\", "/") ?? "");
            }

            // Логируем восстановление
            await _auditService.LogAsync(
                AuditAction.FileRestore,
                userId,
                fileId,
                description: $"Восстановлена версия {version.VersionNumber} файла {file.Name}",
                isSuccess: true);

            _logger.LogInformation("Restored version {VersionNumber} for file {FileId} by user {UserId}",
                version.VersionNumber, fileId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring version {VersionId} for file {FileId}", versionId, fileId);

            await _auditService.LogAsync(
                AuditAction.FileRestore,
                userId,
                fileId,
                description: $"Ошибка восстановления версии {version.VersionNumber} файла {file.Name}",
                isSuccess: false,
                errorMessage: ex.Message);

            return false;
        }
    }

    public async Task CleanupOldVersionsAsync(Guid fileId)
    {
        const int maxVersionsPerFile = 10; // Можно вынести в настройки

        var versions = await _context.FileVersions
            .Where(v => v.FileId == fileId)
            .OrderByDescending(v => v.VersionNumber)
            .Skip(maxVersionsPerFile)
            .ToListAsync();

        foreach (var version in versions)
        {
            // Удаляем физический файл
            if (version.LocalArchivePath != null && File.Exists(version.LocalArchivePath))
            {
                try
                {
                    File.Delete(version.LocalArchivePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deleting archive file {ArchivePath}", version.LocalArchivePath);
                }
            }

            _context.FileVersions.Remove(version);
        }

        if (versions.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleaned up {Count} old versions for file {FileId}", versions.Count, fileId);
        }
    }

    private FileVersionDto MapToDto(FileVersion version, Domain.Entities.User? user)
    {
        return new FileVersionDto
        {
            Id = version.Id,
            FileId = version.FileId,
            VersionNumber = version.VersionNumber,
            LocalArchivePath = version.LocalArchivePath,
            SizeBytes = version.SizeBytes,
            Comment = version.Comment,
            CreatedById = version.CreatedById,
            CreatedByName = user?.FullName ?? "",
            CreatedAt = version.CreatedAt,
            IsCurrentVersion = version.IsCurrentVersion
        };
    }
}