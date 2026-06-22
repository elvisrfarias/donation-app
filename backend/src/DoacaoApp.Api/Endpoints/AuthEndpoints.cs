using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Authentication.Commands;
using DoacaoApp.Application.Common.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace DoacaoApp.Api.Endpoints;

/// <summary>Endpoints de autenticação (Minimal API). Protegidos por rate limiting.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Autenticação");

        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).Match(value => Results.Ok(value)))
            .AllowAnonymous()
            .Produces<AuthResult>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Autentica por e-mail/senha e retorna access token + refresh token.");

        group.MapPost("/register", async (RegisterUserCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).Match(value => Results.Ok(value)))
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin })
            .Produces<AuthResult>()
            .WithSummary("Cria um novo usuário gerencial (somente Admin).");

        group.MapPost("/refresh", async (RefreshTokenCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).Match(value => Results.Ok(value)))
            .AllowAnonymous()
            .Produces<AuthResult>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Rotaciona um refresh token válido por um novo par de tokens.");

        group.MapPost("/revoke", async (RevokeTokenCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).Match(_ => Results.NoContent()))
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Revoga um refresh token (logout).");

        return app;
    }
}