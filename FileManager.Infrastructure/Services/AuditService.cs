using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using FileManager.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace FileManager.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(AppDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(AuditAction action, Guid? userId = null, Guid? fileId = null,
                              Guid? folderId = null, string description = "",
                              string? ipAddress = null, bool isSuccess = true,
                              string? errorMessage = null)
    {
        try
        {
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

            _logger.LogInformation("Audit log created: {Action} by {UserId} for {FileId}/{FolderId}",
                action, userId, fileId, folderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for action {Action}", action);
        }
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(DateTime? from = null, DateTime? to = null,
                                                         Guid? userId = null, AuditAction? action = null)
    {
        var query = _context.AuditLogs.AsQueryable();

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