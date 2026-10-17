using ErrorOr;

namespace DoacaoApp.Application.Authentication;

/// <summary>Erros do fluxo de autenticação/identidade.</summary>
public static class AuthErrors
{
    public static Error InvalidCredentials =>
        Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha inválidos.");

    public static Error InvalidRefreshToken =>
        Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token inválido, expirado ou revogado.");

    public static Error EmailAlreadyUsed =>
        Error.Conflict("Auth.EmailAlreadyUsed", "Já existe um usuário com este e-mail.");

    public static Error InvalidRole =>
        Error.Validation("Auth.InvalidRole", "Role inválida. Use: Admin, PointManager ou Viewer.");

    public static Error UserNotFound =>
        Error.NotFound("Auth.UserNotFound", "Usuário não encontrado.");

    public static Error InsufficientPermissionsToAssignRole =>
        Error.Forbidden("Auth.InsufficientPermissionsToAssignRole", "Você não tem permissão para atribuir este papel.");

    public static Error RegistrationFailed(string detail) =>
        Error.Validation("Auth.RegistrationFailed", detail);

    public static Error InvalidCurrentPassword =>
        Error.Validation("Auth.InvalidCurrentPassword", "A senha atual não confere.");

    public static Error PasswordRejected(string detail) =>
        Error.Validation("Auth.PasswordRejected", detail);

    public static Error NotAuthenticated =>
        Error.Unauthorized("Auth.NotAuthenticated", "Sessão inválida. Faça login novamente.");
}