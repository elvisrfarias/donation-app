namespace DoacaoApp.Infrastructure.Identity;

/// <summary>
/// Token de uso único do fluxo "esqueci minha senha". Só o hash SHA-256 é persistido; o valor em claro existe
/// apenas no link enviado por e-mail.
/// </summary>
public sealed class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }

    public bool IsUsable(DateTime nowUtc) => UsedAtUtc is null && nowUtc < ExpiresAtUtc;
}
