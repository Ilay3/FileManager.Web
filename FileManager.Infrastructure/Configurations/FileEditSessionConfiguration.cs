using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManager.Infrastructure.Configurations;

public class FileEditSessionConfiguration : IEntityTypeConfiguration<FileEditSession>
{
    public void Configure(EntityTypeBuilder<FileEditSession> builder)
    {
        builder.ToTable("file_edit_sessions");

        builder.HasKey(fes => fes.Id);

        builder.Property(fes => fes.YandexEditUrl)
            .HasMaxLength(1000);

        builder.Property(fes => fes.IpAddress)
            .HasMaxLength(45); // IPv6

        builder.Property(fes => fes.UserAgent)
            .HasMaxLength(500);

        // Связи
        builder.HasOne(fes => fes.File)
            .WithMany()
            .HasForeignKey(fes => fes.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fes => fes.User)
            .WithMany()
            .HasForeignKey(fes => fes.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Индексы
        builder.HasIndex(fes => fes.FileId)
            .HasDatabaseName("IX_FileEditSessions_FileId");

        builder.HasIndex(fes => fes.UserId)
            .HasDatabaseName("IX_FileEditSessions_UserId");

        builder.HasIndex(fes => fes.StartedAt)
            .HasDatabaseName("IX_FileEditSessions_StartedAt");

        builder.HasIndex(fes => new { fes.FileId, fes.StartedAt })
            .HasDatabaseName("IX_FileEditSessions_FileId_StartedAt");
    }
}