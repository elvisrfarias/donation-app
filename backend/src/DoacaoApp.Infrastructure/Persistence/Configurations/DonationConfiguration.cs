using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Donations;
using DoacaoApp.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("donations");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.PointId).HasColumnName("collection_point_id").IsRequired();
        builder.Property(d => d.DonorName)
            .HasColumnName("donor_name")
            .IsRequired()
            .HasMaxLength(Donation.MaxDonorNameLength);
        builder.Property(d => d.DonorPhone)
            .HasColumnName("donor_phone")
            .HasMaxLength(11)
            .HasConversion(phone => phone!.Value, value => PhoneNumber.FromTrusted(value));
        builder.Property(d => d.Description)
            .HasColumnName("description")
            .IsRequired()
            .HasMaxLength(Donation.MaxDescriptionLength);
        builder.Property(d => d.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(16)
            .HasConversion<string>();
        builder.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(d => d.ClosedAtUtc).HasColumnName("closed_at_utc");

        builder.HasOne<CollectionPoint>()
            .WithMany()
            .HasForeignKey(d => d.PointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.PointId, d.CreatedAtUtc });
    }
}
