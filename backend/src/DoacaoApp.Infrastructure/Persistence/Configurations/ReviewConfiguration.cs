using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.PointId).HasColumnName("collection_point_id").IsRequired();
        builder.Property(r => r.AuthorName)
            .HasColumnName("author_name")
            .IsRequired()
            .HasMaxLength(Review.MaxAuthorNameLength);
        builder.Property(r => r.Rating).HasColumnName("rating").IsRequired();
        builder.Property(r => r.Comment).HasColumnName("comment").HasMaxLength(Review.MaxCommentLength);
        builder.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(r => r.IsHidden).HasColumnName("is_hidden").IsRequired().HasDefaultValue(false);
        builder.Property(r => r.ReportCount).HasColumnName("report_count").IsRequired().HasDefaultValue(0);

        builder.HasIndex(r => r.IsHidden);

        builder.HasOne<CollectionPoint>()
            .WithMany()
            .HasForeignKey(r => r.PointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.PointId);
    }
}
