namespace DoacaoApp.Infrastructure.Identity;

/// <summary>Parâmetros de segurança, seção "Security".</summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>Custo (work factor) do BCrypt. Maior = mais lento e mais seguro.</summary>
    public int BCryptWorkFactor { get; init; } = 12;
}