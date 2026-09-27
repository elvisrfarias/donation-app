using DoacaoApp.Domain.CollectionPoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class BasketTemplateConfiguration : IEntityTypeConfiguration<BasketTemplate>
{
    public void Configure(EntityTypeBuilder<BasketTemplate> builder)
    {
        builder.ToTable("basket_templates");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.PointId).IsRequired();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(120);
        builder.Property(t => t.IsActive).IsRequired();

        builder.OwnsMany(t => t.Items, item =>
        {
            item.ToTable("basket_template_items");
            item.WithOwner().HasForeignKey("basket_template_id");
            item.Property(i => i.StockItemId).HasColumnName("stock_item_id").IsRequired();
            item.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();
            item.HasKey("basket_template_id", nameof(BasketTemplateItem.StockItemId));
            item.HasOne<StockItem>().WithMany().HasForeignKey(i => i.StockItemId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Metadata
            .FindNavigation(nameof(BasketTemplate.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
