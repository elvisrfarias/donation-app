using DoacaoApp.Domain.Families;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("families");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Name).IsRequired().HasMaxLength(200);
        builder.Property(f => f.CreatedAtUtc).IsRequired();

        // WithdrawalCycle (Value Object) inline.
        builder.OwnsOne(f => f.Cycle, cycle =>
        {
            cycle.Property(x => x.Type).HasColumnName("cycle_type").HasConversion<string>().HasMaxLength(20).IsRequired();
            cycle.Property(x => x.MaxWithdrawalsPerCycle).HasColumnName("cycle_max_withdrawals").IsRequired();
        });
        builder.Navigation(f => f.Cycle).IsRequired();

        builder.HasMany(f => f.Beneficiaries)
            .WithOne()
            .HasForeignKey(b => b.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Family.Beneficiaries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}