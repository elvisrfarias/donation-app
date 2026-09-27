using DoacaoApp.Domain.CollectionPoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.PointId).IsRequired();
        builder.Property(i => i.Name).IsRequired().HasMaxLength(120);
        builder.Property(i => i.Unit).IsRequired().HasMaxLength(20);
        builder.Property(i => i.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();
        builder.Property(i => i.Goal).IsRequired();
        builder.Property(i => i.AlertThreshold).IsRequired();
        builder.Property(i => i.IsTracked).IsRequired();

        builder.HasIndex(i => new { i.PointId, i.Name }).IsUnique();
    }
}
