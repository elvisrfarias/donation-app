using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class BeneficiaryConfiguration : IEntityTypeConfiguration<Beneficiary>
{
    public void Configure(EntityTypeBuilder<Beneficiary> builder)
    {
        builder.ToTable("beneficiaries");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);

        builder.Property(b => b.Cpf)
            .HasColumnName("cpf")
            .IsRequired()
            .HasMaxLength(11)
            .HasConversion(cpf => cpf.Value, value => Cpf.FromTrusted(value));

        builder.Property(b => b.BirthDate);
        builder.Property(b => b.RegisteredByPointId).IsRequired();
        builder.Property(b => b.IsActive).IsRequired();
        builder.Property(b => b.CreatedAtUtc).IsRequired();

        builder.HasIndex(b => b.Cpf).IsUnique();

        builder.OwnsMany(b => b.Kinships, kinship =>
        {
            kinship.ToTable("beneficiary_kinships");
            kinship.WithOwner().HasForeignKey("beneficiary_id");
            kinship.Property(k => k.RelatedBeneficiaryId).HasColumnName("related_beneficiary_id").IsRequired();
            kinship.Property(k => k.RelatedBeneficiaryName).HasColumnName("related_beneficiary_name").HasMaxLength(200).IsRequired();
            kinship.Property(k => k.RelatedBeneficiaryCpf)
                .HasColumnName("related_beneficiary_cpf")
                .HasMaxLength(11)
                .IsRequired()
                .HasConversion(cpf => cpf.Value, value => Cpf.FromTrusted(value));
            kinship.Property(k => k.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20).IsRequired();
            kinship.Property(k => k.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            kinship.HasKey("beneficiary_id", nameof(BeneficiaryKinship.RelatedBeneficiaryId));
        });

        builder.Metadata
            .FindNavigation(nameof(Beneficiary.Kinships))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(b => b.PointLinks, link =>
        {
            link.ToTable("beneficiary_point_links");
            link.WithOwner().HasForeignKey("beneficiary_id");
            link.Property(l => l.PointId).HasColumnName("point_id").IsRequired();
            link.Property(l => l.LinkedAtUtc).HasColumnName("linked_at_utc").IsRequired();
            link.HasKey("beneficiary_id", nameof(BeneficiaryPointLink.PointId));
            link.HasIndex(l => l.PointId);
        });

        builder.Metadata
            .FindNavigation(nameof(Beneficiary.PointLinks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}