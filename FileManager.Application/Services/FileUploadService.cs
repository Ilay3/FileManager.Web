using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FileManager.Application.Services;

public class FileUploadService
{
    private readonly IFilesRepository _filesRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IYandexDiskService _yandexDiskService;
    private readonly IAccessService _accessService;
    private readonly IAuditService _auditService;
    private readonly IFileStorageOptions _storageOptions;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(
        IFilesRepository filesRepository,
        IFolderRepository folderRepository,
        IUserRepository userRepository,
        IYandexDiskService yandexDiskService,
        IAccessService accessService,
        IAuditService auditService,
        IFileStorageOptions storageOptions,
        ILogger<FileUploadService> logger)
    {
        _filesRepository = filesRepository;
        _folderRepository = folderRepository;
        _userRepository = userRepository;
        _yandexDiskService = yandexDiskService;
        _accessService = accessService;
        _auditService = auditService;
        _storageOptions = storageOptions;
        _logger = logger;
    }

    public async Task<FileDto> UploadFileAsync(IFormFile file, Guid userId, Guid? folderId = null, string? comment = null)
    {
        // Валидация файла
        var validation = await ValidateFileAsync(file);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Файл не прошел валидацию: {string.Join(", ", validation.Errors)}");
        }

        // Получаем пользователя
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("Пользователь не найден");
        }

        // Определяем папку назначения
        Folder targetFolder;
        if (!folderId.HasValue || folderId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Папка назначения не указана");
        }

        targetFolder = await _folderRepository.GetByIdAsync(folderId.Value)
            ?? throw new InvalidOperationException("Папка назначения не найдена");

        try
        {
            // Генерируем уникальное имя файла
            var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = await GenerateUniqueFileName(originalFileName, extension, targetFolder.Id);

            // Определяем путь в Яндекс.Диске
            var yandexPath = $"{targetFolder.YandexPath}/{uniqueFileName}";

            // Загружаем файл на Яндекс.Диск
            using var stream = file.OpenReadStream();
            // Передаем полный путь как есть, без дополнительной обработки
            var uploadedPath = await _yandexDiskService.UploadFileAsync(stream, uniqueFileName, targetFolder.YandexPath);

            // Создаем запись о файле в БД
            var fileEntity = new Files
            {
                Name = uniqueFileName,
                OriginalName = file.FileName,
                YandexPath = uploadedPath,
                FileType = DetermineFileType(extension),
                Extension = extension,
                SizeBytes = file.Length,
                FolderId = targetFolder.Id,
                UploadedById = userId,
                Tags = ExtractTagsFromFileName(file.FileName)
            };

            fileEntity = await _filesRepository.CreateAsync(fileEntity);

            await _accessService.GrantAccessAsync(fileEntity.Id, null, userId, null,
                AccessType.FullAccess, userId);

            // Логируем действие
            await _auditService.LogAsync(
                AuditAction.FileUpload,
                userId,
                fileEntity.Id,
                targetFolder.Id,
                $"Загружен файл: {file.FileName} -> {uniqueFileName} ({FormatFileSize(file.Length)})",
                null,
                true);

            _logger.LogInformation("File uploaded successfully: {FileName} by user {UserId}",
                uniqueFileName, userId);

            // Возвращаем DTO
            return new FileDto
            {
                Id = fileEntity.Id,
                Name = fileEntity.Name,
                OriginalName = fileEntity.OriginalName,
                Extension = fileEntity.Extension,
                FileType = fileEntity.FileType,
                SizeBytes = fileEntity.SizeBytes,
                CreatedAt = fileEntity.CreatedAt,
                UpdatedAt = fileEntity.UpdatedAt,
                Tags = fileEntity.Tags,
                FolderId = fileEntity.FolderId,
                FolderName = targetFolder.Name,
                UploadedById = userId,
                UploadedByName = user.FullName,
                IsNetworkAvailable = true
            };
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            await _auditService.LogAsync(
                AuditAction.FileUpload,
                userId,
                null,
                folderId,
                $"Ошибка загрузки файла: {file.FileName}",
                null,
                false,
                ex.Message);

            _logger.LogError(ex, "Error uploading file {FileName} by user {UserId}",
                file.FileName, userId);

            throw;
        }
    }

    public async Task<FileValidationResult> ValidateFileAsync(IFormFile file)
    {
        var result = new FileValidationResult();

        // Проверка размера файла
        if (file.Length > _storageOptions.MaxFileSize)
        {
            result.Errors.Add($"Размер файла превышает максимально допустимый ({FormatFileSize(_storageOptions.MaxFileSize)})");
        }

        if (file.Length == 0)
        {
            result.Errors.Add("Файл пустой");
        }

        // Проверка расширения файла
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (_storageOptions.AllowedExtensions.Any() &&
            !_storageOptions.AllowedExtensions.Contains(extension))
        {
            result.Errors.Add($"Тип файла {extension} не разрешен. Разрешенные типы: {string.Join(", ", _storageOptions.AllowedExtensions)}");
        }

        // Проверка имени файла
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            result.Errors.Add("Не указано имя файла");
        }

        // Проверка на потенциально опасные символы в имени файла
        var invalidChars = Path.GetInvalidFileNameChars();
        if (file.FileName.IndexOfAny(invalidChars) >= 0)
        {
            result.Errors.Add("Имя файла содержит недопустимые символы");
        }

        // Предупреждения
        if (file.Length > _storageOptions.MaxFileSize / 2)
        {
            result.Warnings.Add("Файл довольно большой, загрузка может занять некоторое время");
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    private async Task<string> GenerateUniqueFileName(string originalName, string extension, Guid folderId)
    {
        var baseName = SanitizeFileName(originalName);
        var fileName = $"{baseName}{extension}";
        var counter = 1;

        // Проверяем существование файла в этой папке
        while (await FileExistsInFolder(fileName, folderId))
        {
            fileName = $"{baseName}_{counter}{extension}";
            counter++;
        }

        return fileName;
    }

    private async Task<bool> FileExistsInFolder(string fileName, Guid folderId)
    {
        var files = await _filesRepository.GetByFolderIdAsync(folderId);
        return files.Any(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

        // Заменяем пробелы на подчеркивания для лучшей совместимости
        sanitized = sanitized.Replace(' ', '_');

        // Ограничиваем длину
        if (sanitized.Length > 100)
        {
            sanitized = sanitized.Substring(0, 100);
        }

        return sanitized;
    }

    private FileType DetermineFileType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".docx" or ".doc" => FileType.Document,
            ".xlsx" or ".xls" => FileType.Spreadsheet,
            ".pptx" or ".ppt" => FileType.Presentation,
            ".pdf" => FileType.Pdf,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => FileType.Image,
            ".txt" => FileType.Text,
            ".zip" or ".rar" or ".7z" => FileType.Archive,
            _ => FileType.Other
        };
    }

    private string? ExtractTagsFromFileName(string fileName)
    {
        // Простая логика извлечения тегов из имени файла
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var tags = new List<string>();

        // Ищем слова которые могут быть тегами
        if (baseName.Contains("отчет", StringComparison.OrdinalIgnoreCase))
            tags.Add("отчет");
        if (baseName.Contains("презентация", StringComparison.OrdinalIgnoreCase))
            tags.Add("презентация");
        if (baseName.Contains("бюджет", StringComparison.OrdinalIgnoreCase))
            tags.Add("бюджет");
        if (baseName.Contains("план", StringComparison.OrdinalIgnoreCase))
            tags.Add("план");

        return tags.Any() ? string.Join(",", tags) : null;
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} Б";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} КБ";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} МБ";
        return $"{bytes / (1024 * 1024 * 1024):F1} ГБ";
    }
}

public class FileValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}