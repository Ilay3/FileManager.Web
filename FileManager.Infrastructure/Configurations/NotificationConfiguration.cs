using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManager.Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired();

        // Enum как строка
        builder.Property(n => n.RelatedAction)
            .HasConversion<string>();

        // Связи
        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.File)
            .WithMany()
            .HasForeignKey(n => n.FileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.Folder)
            .WithMany()
            .HasForeignKey(n => n.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Индексы
        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("IX_Notifications_UserId");

        builder.HasIndex(n => n.IsSent)
            .HasDatabaseName("IX_Notifications_IsSent");

        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_Notifications_CreatedAt");
    }
}
