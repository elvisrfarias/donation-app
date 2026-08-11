using ErrorOr;

namespace DoacaoApp.Application.Common.Abstractions;

/// <summary>Resultado de uma autenticação bem-sucedida (tokens + dados do usuário).</summary>
public sealed record AuthResult(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    Guid? CollectionPointId,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc);

/// <summary>Dados para registro de um novo usuário gerencial.</summary>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string Role,
    Guid? CollectionPointId);

/// <summary>Um usuário gerencial, para a grid administrativa de usuários.</summary>
public sealed record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    Guid? CollectionPointId,
    bool IsActive);

/// <summary>Dados para atualizar papel, nome e ponto de coleta de um usuário existente.</summary>
public sealed record UpdateUserRequest(
    Guid UserId,
    string FullName,
    string Role,
    Guid? CollectionPointId);

/// <summary>
/// Abstrai o ASP.NET Core Identity + emissão/rotação de JWT e refresh tokens.
/// A implementação (Infrastructure) usa BCrypt para hashing e persiste refresh tokens em banco.
/// </summary>
public interface IIdentityService
{
    Task<ErrorOr<AuthResult>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<ErrorOr<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<ErrorOr<AuthResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<ErrorOr<IReadOnlyList<UserResponse>>> ListUsersAsync(CancellationToken cancellationToken = default);

    Task<ErrorOr<UserResponse>> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
}