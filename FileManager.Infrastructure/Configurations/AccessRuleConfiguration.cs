using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManager.Infrastructure.Configurations;

public class AccessRuleConfiguration : IEntityTypeConfiguration<AccessRule>
{
    public void Configure(EntityTypeBuilder<AccessRule> builder)
    {
        builder.ToTable("access_rules");

        builder.HasKey(ar => ar.Id);

        // Enum как число (флаги)
        builder.Property(ar => ar.AccessType)
            .HasConversion<int>();

        // Связи
        builder.HasOne(ar => ar.File)
            .WithMany(f => f.AccessRules)
            .HasForeignKey(ar => ar.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ar => ar.Folder)
            .WithMany(f => f.AccessRules)
            .HasForeignKey(ar => ar.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ar => ar.User)
            .WithMany(u => u.AccessRules)
            .HasForeignKey(ar => ar.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ar => ar.Group)
            .WithMany(g => g.AccessRules)
            .HasForeignKey(ar => ar.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ar => ar.GrantedBy)
            .WithMany()
            .HasForeignKey(ar => ar.GrantedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Исправленные Check Constraints с правильными именами столбцов PostgreSQL
        builder.HasCheckConstraint("CK_AccessRules_FileOrFolder",
            "(\"FileId\" IS NOT NULL AND \"FolderId\" IS NULL) OR (\"FileId\" IS NULL AND \"FolderId\" IS NOT NULL)");

        builder.HasCheckConstraint("CK_AccessRules_UserOrGroup",
            "(\"UserId\" IS NOT NULL AND \"GroupId\" IS NULL) OR (\"UserId\" IS NULL AND \"GroupId\" IS NOT NULL)");

        // Индексы
        builder.HasIndex(ar => ar.FileId)
            .HasDatabaseName("IX_AccessRules_FileId");

        builder.HasIndex(ar => ar.FolderId)
            .HasDatabaseName("IX_AccessRules_FolderId");

        builder.HasIndex(ar => ar.UserId)
            .HasDatabaseName("IX_AccessRules_UserId");

        builder.HasIndex(ar => ar.GroupId)
            .HasDatabaseName("IX_AccessRules_GroupId");
    }
}
