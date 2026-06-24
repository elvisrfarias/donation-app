using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

        var itemsConverter = new ValueConverter<IReadOnlyList<ItemCategory>, string>(
            items => string.Join(',', items.Select(static i => i.ToString())),
            value => value.Length == 0
                ? new List<ItemCategory>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(Enum.Parse<ItemCategory>)
                    .ToList());

        var itemsComparer = new ValueComparer<IReadOnlyList<ItemCategory>>(
            (a, b) => a!.SequenceEqual(b!),
            items => items.Aggregate(0, (hash, i) => HashCode.Combine(hash, (int)i)),
            items => items.ToList());

        builder.Property(p => p.AcceptedItems)
            .HasColumnName("accepted_items")
            .HasConversion(itemsConverter, itemsComparer)
            .HasMaxLength(300)
            .IsRequired()
            .HasDefaultValue(new List<ItemCategory>());
        builder.Metadata
            .FindProperty(nameof(CollectionPoint.AcceptedItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.Property(p => p.ImageFileId).HasColumnName("image_file_id");

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

        builder.OwnsOne(p => p.Contact, contact =>
        {
            contact.Property(x => x.ResponsibleName).HasColumnName("contact_name").HasMaxLength(160);
            contact.Property(x => x.Email).HasColumnName("contact_email").HasMaxLength(320)
                .HasConversion(email => email.Value, value => Email.FromTrusted(value));
            contact.Property(x => x.Phone).HasColumnName("contact_phone").HasMaxLength(11)
                .HasConversion(phone => phone.Value, value => PhoneNumber.FromTrusted(value));
        });
        builder.Navigation(p => p.Contact).IsRequired();

        builder.HasMany(p => p.Withdrawals)
            .WithOne()
            .HasForeignKey(w => w.PointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(CollectionPoint.Withdrawals))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}