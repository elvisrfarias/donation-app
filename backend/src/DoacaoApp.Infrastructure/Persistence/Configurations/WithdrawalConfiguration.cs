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
        builder.PrimitiveCollection(w => w.Categories)
            .HasColumnName("categories")
            .ElementType()
            .HasConversion<string>();
        builder.Metadata
            .FindProperty(nameof(Withdrawal.Categories))!
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