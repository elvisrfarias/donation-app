using System.Security.Claims;
using DoacaoApp.Application.Common.Abstractions;

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
}