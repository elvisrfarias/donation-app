namespace DoacaoApp.Infrastructure.Persistence.Seed;

/// <summary>Configuração do seed inicial, seção "SeedData".</summary>
public sealed class SeedOptions
{
    public const string SectionName = "SeedData";

    public bool Enabled { get; init; }

    /// <summary>Cria os pontos de coleta de demonstração (só dev; em produção fica sempre desligado).</summary>
    public bool DemoData { get; init; }
    public string AdminEmail { get; init; } = "admin@doacao.local";
    public string AdminPassword { get; init; } = "Admin@123456";
}