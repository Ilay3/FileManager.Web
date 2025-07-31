using FileManager.Domain.Entities;
using FileManager.Domain.Enums;

namespace FileManager.Domain.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditAction action, Guid? userId = null, Guid? fileId = null,
                  Guid? folderId = null, string description = "",
                  string? ipAddress = null, bool isSuccess = true,
                  string? errorMessage = null);

    Task<IEnumerable<AuditLog>> GetLogsAsync(DateTime? from = null, DateTime? to = null,
                                           Guid? userId = null, AuditAction? action = null);
}
