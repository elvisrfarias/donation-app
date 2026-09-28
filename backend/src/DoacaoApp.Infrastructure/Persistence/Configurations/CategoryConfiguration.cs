using DoacaoApp.Domain.CollectionPoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.PointId).IsRequired();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(120);
        builder.Property(c => c.Icon).IsRequired().HasMaxLength(60);

        builder.HasIndex(c => new { c.PointId, c.Name }).IsUnique();
    }
}
