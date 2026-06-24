using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.Storage;
using DoacaoApp.Infrastructure.Identity;
using DoacaoApp.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DoacaoApp.Infrastructure.Persistence;

/// <summary>
/// Contexto único: tabelas do ASP.NET Core Identity + agregados do domínio + refresh tokens.
/// Implementa <see cref="IUnitOfWork"/> (a própria SaveChangesAsync herdada satisfaz o contrato).
/// O dispatch de Domain Events é feito por interceptor (ver DI), não aqui.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public DbSet<CollectionPoint> CollectionPoints => Set<CollectionPoint>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<Family> Families => Set<Family>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<CollectionPointId>().HaveConversion<CollectionPointIdConverter>();
        configurationBuilder.Properties<WithdrawalId>().HaveConversion<WithdrawalIdConverter>();
        configurationBuilder.Properties<FamilyId>().HaveConversion<FamilyIdConverter>();
        configurationBuilder.Properties<BeneficiaryId>().HaveConversion<BeneficiaryIdConverter>();
        configurationBuilder.Properties<StoredFileId>().HaveConversion<StoredFileIdConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}