using Microsoft.AspNetCore.Identity;

namespace DoacaoApp.Infrastructure.Identity;

/// <summary>
/// Usuário gerencial. Estende o Identity com nome completo e o (opcional) ponto de coleta
/// ao qual um PointManager está associado.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    /// <summary>Ponto de coleta vinculado (para PointManager). Null para Admin/Viewer globais.</summary>
    public Guid? CollectionPointId { get; set; }

    public List<RefreshToken> RefreshTokens { get; init; } = [];
}