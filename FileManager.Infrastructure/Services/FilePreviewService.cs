using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using FileManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace FileManager.Application.Services;

public class FilePreviewService : IFilePreviewService
{
    private readonly IFilesRepository _filesRepository;
    private readonly IYandexDiskService _yandexDiskService;
    private readonly IAuditService _auditService;
    private readonly AppDbContext _context;
    private readonly ILogger<FilePreviewService> _logger;

    private readonly string[] _previewableExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".txt", ".docx", ".xlsx", ".pptx" };
    private readonly string[] _editableExtensions = { ".docx", ".xlsx", ".pptx" };

    public FilePreviewService(
        IFilesRepository filesRepository,
        IYandexDiskService yandexDiskService,
        IAuditService auditService,
        AppDbContext context,
        ILogger<FilePreviewService> logger)
    {
        _filesRepository = filesRepository;
        _yandexDiskService = yandexDiskService;
        _auditService = auditService;
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetPreviewUrlAsync(Guid fileId, Guid userId)
    {
        try
        {
            var file = await _filesRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found for preview", fileId);
                return null;
            }

            // TODO: Implement access control check

            if (!CanPreviewAsync(file.Extension).Result)
            {
                _logger.LogWarning("File {FileId} with extension {Extension} cannot be previewed", fileId, file.Extension);
                return null;
            }

            // Логируем действие предпросмотра
            await _auditService.LogAsync(
                AuditAction.FilePreview,
                userId,
                fileId,
                description: $"Preview requested for {file.Name}");

            // Для изображений и PDF возвращаем URL для скачивания
            if (IsImageFile(file.Extension) || file.Extension.ToLower() == ".pdf")
            {
                return $"/api/files/{fileId}/content";
            }

            // Для документов Office возвращаем ссылку на Yandex.Docs
            if (_editableExtensions.Contains(file.Extension.ToLower()))
            {
                var editUrl = await _yandexDiskService.GetEditLinkAsync(file.YandexPath);
                return editUrl;
            }

            // Для текстовых файлов возвращаем URL для получения содержимого
            if (file.Extension.ToLower() == ".txt")
            {
                return $"/api/files/{fileId}/content";
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get preview URL for file {FileId}", fileId);
            return null;
        }
    }

    public async Task<string?> GetEditUrlAsync(Guid fileId, Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var file = await _filesRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found for editing", fileId);
                return null;
            }

            // TODO: Implement access control check

            if (!CanEditOnlineAsync(file.Extension).Result)
            {
                _logger.LogWarning("File {FileId} with extension {Extension} cannot be edited online", fileId, file.Extension);
                return null;
            }

            // Получаем ссылку для редактирования
            var editUrl = await _yandexDiskService.GetEditLinkAsync(file.YandexPath);

            // Создаем сессию редактирования
            var editSession = new FileEditSession
            {
                FileId = fileId,
                UserId = userId,
                YandexEditUrl = editUrl,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.FileEditSessions.Add(editSession);
            await _context.SaveChangesAsync();

            // Логируем действие открытия в редакторе
            await _auditService.LogAsync(
                AuditAction.FileOpenForEdit,
                userId,
                fileId,
                description: $"File {file.Name} opened for editing",
                ipAddress: ipAddress);

            _logger.LogInformation("Edit session {SessionId} created for file {FileId} by user {UserId}",
                editSession.Id, fileId, userId);

            return editUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get edit URL for file {FileId}", fileId);
            return null;
        }
    }

    public async Task<Stream?> GetFileContentAsync(Guid fileId, Guid userId)
    {
        try
        {
            var file = await _filesRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found for content retrieval", fileId);
                return null;
            }

            // TODO: Implement access control check

            // Логируем действие просмотра
            await _auditService.LogAsync(
                AuditAction.FileView,
                userId,
                fileId,
                description: $"Content retrieved for {file.Name}");

            return await _yandexDiskService.DownloadFileAsync(file.YandexPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file content for {FileId}", fileId);
            return null;
        }
    }

    public async Task<FileInfoDto?> GetFileInfoAsync(Guid fileId, Guid userId)
    {
        try
        {
            var file = await _filesRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                return null;
            }

            // TODO: Implement access control check

            return new FileInfoDto
            {
                Id = file.Id,
                Name = file.Name,
                Extension = file.Extension,
                SizeBytes = file.SizeBytes,
                YandexPath = file.YandexPath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file info for {FileId}", fileId);
            return null;
        }
    }

    public Task<bool> CanPreviewAsync(string extension)
    {
        return Task.FromResult(_previewableExtensions.Contains(extension.ToLower()));
    }

    public Task<bool> CanEditOnlineAsync(string extension)
    {
        return Task.FromResult(_editableExtensions.Contains(extension.ToLower()));
    }

    public async Task<List<FileEditSessionDto>> GetActiveEditSessionsAsync(Guid fileId)
    {
        var activeSessions = await _context.FileEditSessions
            .Include(s => s.User)
            .Where(s => s.FileId == fileId && s.EndedAt == null && s.StartedAt > DateTime.UtcNow.AddMinutes(-30))
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();

        return activeSessions.Select(s => new FileEditSessionDto
        {
            Id = s.Id,
            FileId = s.FileId,
            UserId = s.UserId,
            UserName = s.User.FullName,
            StartedAt = s.StartedAt,
            IpAddress = s.IpAddress,
            IsActive = s.IsActive
        }).ToList();
    }

    public async Task EndEditSessionAsync(Guid sessionId, Guid userId)
    {
        try
        {
            var session = await _context.FileEditSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session != null)
            {
                session.EndedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Edit session {SessionId} ended by user {UserId}", sessionId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end edit session {SessionId}", sessionId);
        }
    }

    private bool IsImageFile(string extension)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        return imageExtensions.Contains(extension.ToLower());
    }
}