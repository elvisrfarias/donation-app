namespace DoacaoApp.Application.Common.Abstractions;

/// <summary>Dados do usuário autenticado no request corrente (para auditoria/log).</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }
}