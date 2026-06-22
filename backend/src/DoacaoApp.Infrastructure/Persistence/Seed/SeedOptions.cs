namespace DoacaoApp.Infrastructure.Persistence.Seed;

/// <summary>Configuração do seed inicial, seção "SeedData".</summary>
public sealed class SeedOptions
{
    public const string SectionName = "SeedData";

    public bool Enabled { get; init; }
    public string AdminEmail { get; init; } = "admin@doacao.local";
    public string AdminPassword { get; init; } = "Admin@123456";
}