using FileManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileManager.Infrastructure.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.Description)
            .HasMaxLength(500);

        // Индекс
        builder.HasIndex(g => g.Name)
            .IsUnique()
            .HasDatabaseName("IX_Groups_Name");

        // Many-to-Many с User (через промежуточную таблицу)
        builder.HasMany(g => g.Users)
            .WithMany()
            .UsingEntity("user_groups",
                l => l.HasOne(typeof(User)).WithMany().HasForeignKey("user_id"),
                r => r.HasOne(typeof(Group)).WithMany().HasForeignKey("group_id"));
    }
}
