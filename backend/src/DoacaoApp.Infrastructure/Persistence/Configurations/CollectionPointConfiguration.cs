using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class CollectionPointConfiguration : IEntityTypeConfiguration<CollectionPoint>
{
    public void Configure(EntityTypeBuilder<CollectionPoint> builder)
    {
        builder.ToTable("collection_points");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        // Address (Value Object) inline como owned type.
        builder.OwnsOne(p => p.Address, address =>
        {
            address.Property(x => x.Street).HasColumnName("address_street").IsRequired().HasMaxLength(200);
            address.Property(x => x.Number).HasColumnName("address_number").HasMaxLength(20);
            address.Property(x => x.Complement).HasColumnName("address_complement").HasMaxLength(120);
            address.Property(x => x.District).HasColumnName("address_district").HasMaxLength(120);
            address.Property(x => x.City).HasColumnName("address_city").IsRequired().HasMaxLength(120);
            address.Property(x => x.State).HasColumnName("address_state").IsRequired().HasMaxLength(2);
            address.Property(x => x.ZipCode).HasColumnName("address_zipcode").IsRequired().HasMaxLength(8);
        });
        builder.Navigation(p => p.Address).IsRequired();

        // ContactInfo (Value Object) inline; Email/Phone convertidos para string.
        builder.OwnsOne(p => p.Contact, contact =>
        {
            contact.Property(x => x.ResponsibleName).HasColumnName("contact_name").HasMaxLength(160);
            contact.Property(x => x.Email).HasColumnName("contact_email").HasMaxLength(320)
                .HasConversion(email => email.Value, value => Email.FromTrusted(value));
            contact.Property(x => x.Phone).HasColumnName("contact_phone").HasMaxLength(11)
                .HasConversion(phone => phone.Value, value => PhoneNumber.FromTrusted(value));
        });
        builder.Navigation(p => p.Contact).IsRequired();

        // Withdrawals: entidades do agregado, acessadas pelo campo privado _withdrawals.
        builder.HasMany(p => p.Withdrawals)
            .WithOne()
            .HasForeignKey(w => w.PointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(CollectionPoint.Withdrawals))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}