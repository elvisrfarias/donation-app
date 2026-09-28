using DoacaoApp.Domain.CollectionPoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class WithdrawalConfiguration : IEntityTypeConfiguration<Withdrawal>
{
    public void Configure(EntityTypeBuilder<Withdrawal> builder)
    {
        builder.ToTable("withdrawals");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.PointId).IsRequired();
        builder.Property(w => w.BeneficiaryId).IsRequired();

        builder.OwnsMany(w => w.Items, item =>
        {
            item.ToTable("withdrawal_items");
            item.WithOwner().HasForeignKey("withdrawal_id");
            item.Property(i => i.StockItemId).HasColumnName("stock_item_id").IsRequired();
            item.Property(i => i.Quantity).HasColumnName("quantity").IsRequired();
            item.HasKey("withdrawal_id", nameof(WithdrawalItem.StockItemId));
            item.HasOne<StockItem>().WithMany().HasForeignKey(i => i.StockItemId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Metadata
            .FindNavigation(nameof(Withdrawal.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(w => w.OccurredAtUtc).IsRequired();
        builder.Property(w => w.Notes).HasMaxLength(500);

        builder.Property(w => w.BasketTemplateId).HasColumnName("basket_template_id");
        builder.Property(w => w.BasketQuantity).HasColumnName("basket_quantity");

        builder.HasOne<BasketTemplate>()
            .WithMany()
            .HasForeignKey(w => w.BasketTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => new { w.BeneficiaryId, w.OccurredAtUtc });
        builder.HasIndex(w => w.PointId);
    }
}