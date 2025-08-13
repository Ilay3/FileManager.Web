using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Group> Groups { get; }
    DbSet<Folder> Folders { get; }
    DbSet<Files> Files { get; }
    DbSet<FileVersion> FileVersions { get; }
    DbSet<AccessRule> AccessRules { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<FileEditSession> FileEditSessions { get; }
    DbSet<Favorite> Favorites { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
