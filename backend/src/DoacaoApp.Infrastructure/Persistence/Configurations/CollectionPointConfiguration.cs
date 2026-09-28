using System.Text.Json;
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
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(CollectionPoint.MaxDescriptionLength);
        builder.Property(p => p.OpeningHours).HasColumnName("opening_hours").HasMaxLength(CollectionPoint.MaxOpeningHoursLength);

        builder.OwnsOne(p => p.WithdrawalRule, rule =>
        {
            rule.Property(x => x.Type).HasColumnName("withdrawal_cycle_type").HasConversion<string>().HasMaxLength(20).IsRequired();
            rule.Property(x => x.MaxWithdrawalsPerCycle).HasColumnName("withdrawal_cycle_max").IsRequired();
        });
        builder.Navigation(p => p.WithdrawalRule).IsRequired();

        var needsConverter = new ValueConverter<IReadOnlyList<string>, string>(
            needs => JsonSerializer.Serialize(needs, (JsonSerializerOptions?)null),
            value => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null) ?? new List<string>());

        var needsComparer = new ValueComparer<IReadOnlyList<string>>(
            (a, b) => a!.SequenceEqual(b!),
            needs => needs.Aggregate(0, (hash, n) => HashCode.Combine(hash, n.GetHashCode())),
            needs => needs.ToList());

        builder.Property(p => p.Needs)
            .HasColumnName("needs")
            .HasConversion(needsConverter, needsComparer)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'::jsonb");
        builder.Metadata
            .FindProperty(nameof(CollectionPoint.Needs))!
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
            address.Property(x => x.Latitude).HasColumnName("address_latitude");
            address.Property(x => x.Longitude).HasColumnName("address_longitude");
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

        builder.HasMany(p => p.StockItems)
            .WithOne()
            .HasForeignKey(i => i.PointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(CollectionPoint.StockItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.BasketTemplates)
            .WithOne()
            .HasForeignKey(t => t.PointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(CollectionPoint.BasketTemplates))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Categories)
            .WithOne()
            .HasForeignKey(c => c.PointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(CollectionPoint.Categories))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}