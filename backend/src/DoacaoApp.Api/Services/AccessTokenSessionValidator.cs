using System.Security.Claims;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Common.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace DoacaoApp.Api.Services;

/// <summary>
/// Complementa a validação criptográfica do JWT: recusa o token quando o usuário não existe mais, está
/// bloqueado/desativado ou teve o security stamp trocado (senha, papel ou ponto alterados) depois da emissão.
/// Custa uma busca do usuário por Id a cada requisição autenticada.
/// </summary>
public static class AccessTokenSessionValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var securityStamp = principal?.FindFirstValue(AuthClaimTypes.SecurityStamp);

        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || string.IsNullOrEmpty(securityStamp))
        {
            context.Fail("Token sem identificação de sessão.");
            return;
        }

        var identityService = context.HttpContext.RequestServices.GetRequiredService<IIdentityService>();
        if (!await identityService.IsSessionValidAsync(userId, securityStamp, context.HttpContext.RequestAborted))
        {
            context.Fail("Sessão encerrada: usuário desativado ou credenciais alteradas.");
        }
    }
}
