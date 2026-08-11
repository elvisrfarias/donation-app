using System.Security.Claims;
using DoacaoApp.Application.Common.Abstractions;
using RoleNames = DoacaoApp.Application.Authentication.Roles;

namespace DoacaoApp.Api.Services;

/// <summary>Lê os dados do usuário autenticado a partir do <see cref="HttpContext"/>.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue("email");

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    public Guid? CollectionPointId =>
        Guid.TryParse(Principal?.FindFirstValue("collection_point_id"), out var id) ? id : null;

    public bool IsAdmin => Roles.Contains(RoleNames.Admin);
}