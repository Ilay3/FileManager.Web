using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManager.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Department)
            .HasMaxLength(100);

        // Новые поля для блокировки
        builder.Property(u => u.LockReason)
            .HasMaxLength(500);

        // Поля для сброса пароля
        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(256);

        builder.Property(u => u.LastIpAddress)
            .HasMaxLength(45); // IPv6

        builder.Property(u => u.EmailConfirmationCode)
            .HasMaxLength(10);

        // Индексы
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        builder.HasIndex(u => u.IsLocked)
            .HasDatabaseName("IX_Users_IsLocked");

        builder.HasIndex(u => u.PasswordResetToken)
            .HasDatabaseName("IX_Users_PasswordResetToken");

        builder.HasIndex(u => u.LastActivityAt)
            .HasDatabaseName("IX_Users_LastActivityAt");

        builder.HasIndex(u => u.IsEmailConfirmed)
            .HasDatabaseName("IX_Users_IsEmailConfirmed");

        // Отношения
        builder.HasMany(u => u.UploadedFiles)
            .WithOne(f => f.UploadedBy)
            .HasForeignKey(f => f.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.CreatedFolders)
            .WithOne(f => f.CreatedBy)
            .HasForeignKey(f => f.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Favorites)
            .WithOne(f => f.User)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}