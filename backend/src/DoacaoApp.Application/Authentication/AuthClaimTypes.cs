namespace DoacaoApp.Application.Authentication;

/// <summary>Claims próprias do access token, compartilhadas entre quem emite (Infrastructure) e quem valida (API).</summary>
public static class AuthClaimTypes
{
    /// <summary>Security stamp do usuário no momento da emissão; muda ao trocar senha, papel ou desativar.</summary>
    public const string SecurityStamp = "security_stamp";
}
