using DoacaoApp.Domain.Sponsors;
using DoacaoApp.Domain.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class SponsorConfiguration : IEntityTypeConfiguration<Sponsor>
{
    public void Configure(EntityTypeBuilder<Sponsor> builder)
    {
        builder.ToTable("sponsors");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name).HasColumnName("name").IsRequired().HasMaxLength(Sponsor.MaxNameLength);
        builder.Property(s => s.Tier)
            .HasColumnName("tier")
            .IsRequired()
            .HasMaxLength(16)
            .HasConversion<string>();
        builder.Property(s => s.WebsiteUrl).HasColumnName("website_url").HasMaxLength(Sponsor.MaxWebsiteUrlLength);
        builder.Property(s => s.LogoFileId).HasColumnName("logo_file_id").IsRequired();
        builder.Property(s => s.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);
        builder.Property(s => s.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(s => s.LogoFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.IsActive, s.Tier });
    }
}
