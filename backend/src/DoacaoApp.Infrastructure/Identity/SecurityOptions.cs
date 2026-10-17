namespace DoacaoApp.Infrastructure.Identity;

/// <summary>Parâmetros de segurança, seção "Security".</summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>Custo (work factor) do BCrypt. Maior = mais lento e mais seguro.</summary>
    public int BCryptWorkFactor { get; init; } = 12;

    /// <summary>Validade do link de "esqueci minha senha".</summary>
    public int PasswordResetTokenMinutes { get; init; } = 60;

    /// <summary>Intervalo mínimo entre dois e-mails de redefinição para o mesmo usuário.</summary>
    public int PasswordResetCooldownSeconds { get; init; } = 60;
}