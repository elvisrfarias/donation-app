using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Notices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class NoticeConfiguration : IEntityTypeConfiguration<PointNotice>
{
    public void Configure(EntityTypeBuilder<PointNotice> builder)
    {
        builder.ToTable("point_notices");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.PointId).HasColumnName("collection_point_id").IsRequired();
        builder.Property(n => n.Title).HasColumnName("title").IsRequired().HasMaxLength(PointNotice.MaxTitleLength);
        builder.Property(n => n.Message).HasColumnName("message").IsRequired().HasMaxLength(PointNotice.MaxMessageLength);
        builder.Property(n => n.Audience)
            .HasColumnName("audience")
            .IsRequired()
            .HasMaxLength(16)
            .HasConversion<string>();
        builder.Property(n => n.AuthorName)
            .HasColumnName("author_name")
            .IsRequired()
            .HasMaxLength(PointNotice.MaxAuthorNameLength);
        builder.Property(n => n.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(n => n.IsArchived).HasColumnName("is_archived").IsRequired().HasDefaultValue(false);

        builder.HasOne<CollectionPoint>()
            .WithMany()
            .HasForeignKey(n => n.PointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.PointId, n.IsArchived, n.CreatedAtUtc });
    }
}
