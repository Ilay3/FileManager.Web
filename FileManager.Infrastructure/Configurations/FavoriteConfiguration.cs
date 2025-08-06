using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManager.Infrastructure.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("favorites");

        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.File)
            .WithMany()
            .HasForeignKey(f => f.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Folder)
            .WithMany()
            .HasForeignKey(f => f.FolderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.UserId, f.FileId })
            .IsUnique()
            .HasDatabaseName("IX_Favorites_User_File");

        builder.HasIndex(f => new { f.UserId, f.FolderId })
            .IsUnique()
            .HasDatabaseName("IX_Favorites_User_Folder");
    }
}
