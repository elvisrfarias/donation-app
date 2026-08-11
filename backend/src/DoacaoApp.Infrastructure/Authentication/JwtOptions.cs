namespace DoacaoApp.Infrastructure.Authentication;

/// <summary>Configuração do JWT, vinda da seção "Jwt" (appsettings/env).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public int AccessTokenHours { get; init; } = 4;
    public int RefreshTokenHours { get; init; } = 8;
}