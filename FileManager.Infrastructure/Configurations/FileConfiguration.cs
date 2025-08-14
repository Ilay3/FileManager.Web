using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManager.Infrastructure.Configurations;

public class FileConfiguration : IEntityTypeConfiguration<Files>
{
    public void Configure(EntityTypeBuilder<Files> builder)
    {
        builder.ToTable("files");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(f => f.OriginalName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(f => f.YandexPath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(f => f.Extension)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(f => f.Tags)
            .HasMaxLength(500);

        // Enum как строка
        builder.Property(f => f.FileType)
            .HasConversion<string>();

        // Связи
        builder.HasOne(f => f.Folder)
            .WithMany(folder => folder.Files)
            .HasForeignKey(f => f.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.UploadedBy)
            .WithMany(u => u.UploadedFiles)
            .HasForeignKey(f => f.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Убираем проблемную связь CurrentVersion - настроим позже через Fluent API
        builder.Ignore(f => f.CurrentVersion);

        // Индексы
        builder.HasIndex(f => f.YandexPath)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Files_YandexPath");

        builder.HasIndex(f => new { f.FolderId, f.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Files_FolderId_Name");

        builder.HasIndex(f => f.FolderId)
            .HasDatabaseName("IX_Files_FolderId");

        builder.HasIndex(f => f.FileType)
            .HasDatabaseName("IX_Files_FileType");
    }
}
