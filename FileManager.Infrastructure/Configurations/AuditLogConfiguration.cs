using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManager.Infrastructure.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45); // IPv6

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);

        builder.Property(al => al.ErrorMessage)
            .HasMaxLength(1000);

        // Enum как строка
        builder.Property(al => al.Action)
            .HasConversion<string>();

        // Связи
        builder.HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(al => al.File)
            .WithMany(f => f.AuditLogs)
            .HasForeignKey(al => al.FileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(al => al.Folder)
            .WithMany()
            .HasForeignKey(al => al.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Индексы для быстрого поиска в журнале
        builder.HasIndex(al => al.CreatedAt)
            .HasDatabaseName("IX_AuditLogs_CreatedAt");

        builder.HasIndex(al => al.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(al => al.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(al => al.IsSuccess)
            .HasDatabaseName("IX_AuditLogs_IsSuccess");
    }
}

