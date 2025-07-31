using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManager.Infrastructure.Configurations;

public class FileVersionConfiguration : IEntityTypeConfiguration<FileVersion>
{
    public void Configure(EntityTypeBuilder<FileVersion> builder)
    {
        builder.ToTable("file_versions");

        builder.HasKey(fv => fv.Id);

        builder.Property(fv => fv.LocalArchivePath)
            .HasMaxLength(1000);

        builder.Property(fv => fv.Comment)
            .HasMaxLength(500);

        // Связи
        builder.HasOne(fv => fv.File)
            .WithMany(f => f.Versions)
            .HasForeignKey(fv => fv.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fv => fv.CreatedBy)
            .WithMany(u => u.FileVersions)
            .HasForeignKey(fv => fv.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Индексы
        builder.HasIndex(fv => fv.FileId)
            .HasDatabaseName("IX_FileVersions_FileId");

        builder.HasIndex(fv => new { fv.FileId, fv.VersionNumber })
            .IsUnique()
            .HasDatabaseName("IX_FileVersions_FileId_VersionNumber");
    }
}
