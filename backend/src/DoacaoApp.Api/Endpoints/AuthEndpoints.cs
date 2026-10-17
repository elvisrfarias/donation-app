using DoacaoApp.Api.Common;
using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Authentication.Commands;
using DoacaoApp.Application.Authentication.Queries;
using DoacaoApp.Application.Common.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace DoacaoApp.Api.Endpoints;

/// <summary>Corpo de uma requisição de atualização de usuário (o Id vem na rota).</summary>
public sealed record UpdateUserRequestBody(string FullName, string Role, Guid? CollectionPointId);

/// <summary>Corpo de uma redefinição administrativa de senha (o Id vem na rota).</summary>
public sealed record ResetPasswordRequestBody(string NewPassword);

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
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Roles.Admin},{Roles.PointManager}" })
            .Produces<UserResponse>()
            .ProducesValidationProblem()
            .WithSummary("Cria um novo usuário gerencial (Admin cria qualquer papel; PointManager cria PointManager/Viewer no seu próprio ponto).");

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

        group.MapPost("/change-password", async (ChangePasswordCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).Match(value => Results.Ok(value)))
            .RequireAuthorization()
            .Produces<AuthResult>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithSummary("Troca a senha do usuário autenticado, encerra as demais sessões e devolve um novo par de tokens.");

        group.MapGet("/users", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListUsersQuery(), ct)).Match(value => Results.Ok(value)))
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Roles.Admin},{Roles.PointManager}" })
            .Produces<IReadOnlyList<UserResponse>>()
            .WithSummary("Lista os usuários gerenciais (Admin vê todos; PointManager só vê os do seu próprio ponto).");

        group.MapPut("/users/{id:guid}", async (Guid id, UpdateUserRequestBody body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateUserCommand(id, body.FullName, body.Role, body.CollectionPointId), ct))
                    .Match(value => Results.Ok(value)))
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Roles.Admin},{Roles.PointManager}" })
            .Produces<UserResponse>()
            .ProducesValidationProblem()
            .WithSummary("Atualiza nome, papel e ponto de coleta de um usuário (Admin edita qualquer um; PointManager só o seu próprio ponto).");

        group.MapPost("/users/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeactivateUserCommand(id), ct)).Match(_ => Results.NoContent()))
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Roles.Admin},{Roles.PointManager}" })
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Desativa um usuário, bloqueando novos logins (Admin qualquer um; PointManager só do seu próprio ponto).");

        group.MapPost("/users/{id:guid}/reset-password", async (Guid id, ResetPasswordRequestBody body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ResetUserPasswordCommand(id, body.NewPassword), ct)).Match(_ => Results.NoContent()))
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{Roles.Admin},{Roles.PointManager}" })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .WithSummary("Redefine a senha de um usuário e encerra as sessões dele (Admin qualquer um; PointManager só do seu próprio ponto).");

        return app;
    }
}