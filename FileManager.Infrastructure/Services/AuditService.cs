using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using FileManager.Infrastructure.Data;
using FileManager.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileManager.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly AuditOptions _options;

    public AuditService(AppDbContext context, IOptions<AuditOptions> options, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
    }

    public async Task LogAsync(AuditAction action, Guid? userId = null, Guid? fileId = null,
                              Guid? folderId = null, string description = "",
                              string? ipAddress = null, bool isSuccess = true,
                              string? errorMessage = null)
    {
        try
        {
            if (IsDisabled(action))
                return;

            var auditLog = new AuditLog
            {
                Action = action,
                UserId = userId,
                FileId = fileId,
                FolderId = folderId,
                Description = description,
                IpAddress = ipAddress,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.Log(_options.LogLevel,
                "Audit log created: {Action} by {UserId} for {FileId}/{FolderId}",
                action, userId, fileId, folderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for action {Action}", action);
        }
    }

    private bool IsDisabled(AuditAction action)
    {
        bool isFile = action is AuditAction.FileUpload or AuditAction.FileDownload or AuditAction.FileView or
                       AuditAction.FileEdit or AuditAction.FileDelete or AuditAction.FileRestore or
                       AuditAction.FilePreview or AuditAction.FileOpenForEdit;
        bool isUser = action is AuditAction.Login or AuditAction.Logout;
        bool isAccess = action is AuditAction.AccessGranted or AuditAction.AccessRevoked or AuditAction.AccessChanged;

        if (isFile && !_options.EnableFileActions) return true;
        if (isUser && !_options.EnableUserActions) return true;
        if (isAccess && !_options.EnableAccessLog) return true;

        return false;
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(DateTime? from = null, DateTime? to = null,
                                                         Guid? userId = null, AuditAction? action = null)
    {
        var query = _context.AuditLogs
            .Include(l => l.User)
            .Include(l => l.File)
            .Include(l => l.Folder)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(log => log.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(log => log.CreatedAt <= to.Value);

        if (userId.HasValue)
            query = query.Where(log => log.UserId == userId.Value);

        if (action.HasValue)
            query = query.Where(log => log.Action == action.Value);

        return query.OrderByDescending(log => log.CreatedAt).Take(1000).ToList();
    }
}