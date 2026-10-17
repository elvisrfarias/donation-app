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

/// <summary>Dados para o próprio usuário trocar a senha.</summary>
public sealed record ChangePasswordRequest(
    Guid UserId,
    string CurrentPassword,
    string NewPassword);

/// <summary>
/// Abstrai o ASP.NET Core Identity + emissão/rotação de JWT e refresh tokens.
/// A implementação (Infrastructure) usa BCrypt para hashing e persiste refresh tokens em banco.
/// </summary>
public interface IIdentityService
{
    Task<ErrorOr<UserResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<ErrorOr<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<ErrorOr<AuthResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<ErrorOr<IReadOnlyList<UserResponse>>> ListUsersAsync(CancellationToken cancellationToken = default);

    Task<ErrorOr<UserResponse>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ErrorOr<UserResponse>> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Troca a senha do próprio usuário (exige a atual), encerra as demais sessões e devolve um novo par de
    /// tokens para a sessão corrente continuar ativa.
    /// </summary>
    Task<ErrorOr<AuthResult>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Define uma nova senha para outro usuário (ação administrativa) e encerra todas as sessões dele.</summary>
    Task<ErrorOr<Success>> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica se um access token ainda representa uma sessão válida: o usuário existe, não está bloqueado e o
    /// security stamp do token é o atual.
    /// </summary>
    Task<bool> IsSessionValidAsync(Guid userId, string securityStamp, CancellationToken cancellationToken = default);
}