using DoacaoApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Token).IsRequired().HasMaxLength(200);
        builder.HasIndex(t => t.Token).IsUnique();

        builder.Property(t => t.ExpiresAtUtc).IsRequired();
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.ReplacedByToken).HasMaxLength(200);

        builder.HasOne<ApplicationUser>()
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}