using DoacaoApp.Domain.Families;
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

        builder.Property(b => b.FamilyId).IsRequired();
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

        // Unicidade municipal de CPF reforçada no banco.
        builder.HasIndex(b => b.Cpf).IsUnique();
    }
}