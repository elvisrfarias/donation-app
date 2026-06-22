namespace DoacaoApp.Infrastructure.Identity;

/// <summary>
/// Refresh token persistido em banco (permite revogação e rotação — o JWT de acesso
/// é stateless e curto; o refresh é stateful e auditável).
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsRevoked => RevokedAtUtc is not null;

    public bool IsActive(DateTime nowUtc) => !IsRevoked && nowUtc < ExpiresAtUtc;
}