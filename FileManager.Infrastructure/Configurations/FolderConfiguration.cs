using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManager.Infrastructure.Configurations;

public class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.ToTable("folders");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(f => f.YandexPath)
            .IsRequired()
            .HasMaxLength(1000);

        // Самоссылающаяся связь (древовидная структура)
        builder.HasOne(f => f.ParentFolder)
            .WithMany(f => f.SubFolders)
            .HasForeignKey(f => f.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Связь с создателем
        builder.HasOne(f => f.CreatedBy)
            .WithMany(u => u.CreatedFolders)
            .HasForeignKey(f => f.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Индексы
        builder.HasIndex(f => f.YandexPath)
            .IsUnique()
            .HasDatabaseName("IX_Folders_YandexPath");

        builder.HasIndex(f => f.ParentFolderId)
            .HasDatabaseName("IX_Folders_ParentFolderId");
    }
}
