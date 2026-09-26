using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.ValueObjects;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

        builder.Property(b => b.Email)
            .HasColumnName("email")
            .HasMaxLength(Email.MaxLength)
            .HasConversion(email => email!.Value, value => Email.FromTrusted(value));

        var phonesConverter = new ValueConverter<IReadOnlyList<PhoneNumber>, string>(
            phones => JsonSerializer.Serialize(phones.Select(p => p.Value), (JsonSerializerOptions?)null),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<PhoneNumber>()
                : (JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null) ?? new List<string>())
                    .Select(PhoneNumber.FromTrusted)
                    .ToList());

        var phonesComparer = new ValueComparer<IReadOnlyList<PhoneNumber>>(
            (a, b) => a!.SequenceEqual(b!),
            phones => phones.Aggregate(0, (hash, p) => HashCode.Combine(hash, p.GetHashCode())),
            phones => phones.ToList());

        builder.Property(b => b.Phones)
            .HasColumnName("phones")
            .HasConversion(phonesConverter, phonesComparer)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'::jsonb");
        builder.Metadata
            .FindProperty(nameof(Beneficiary.Phones))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsOne(b => b.Address, address =>
        {
            address.Property(x => x.Street).HasColumnName("address_street").HasMaxLength(200);
            address.Property(x => x.Number).HasColumnName("address_number").HasMaxLength(20);
            address.Property(x => x.Complement).HasColumnName("address_complement").HasMaxLength(120);
            address.Property(x => x.District).HasColumnName("address_district").HasMaxLength(120);
            address.Property(x => x.City).HasColumnName("address_city").HasMaxLength(120);
            address.Property(x => x.State).HasColumnName("address_state").HasMaxLength(2);
            address.Property(x => x.ZipCode).HasColumnName("address_zipcode").HasMaxLength(8);
            address.Property(x => x.Latitude).HasColumnName("address_latitude");
            address.Property(x => x.Longitude).HasColumnName("address_longitude");
        });

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