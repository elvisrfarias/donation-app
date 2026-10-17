namespace DoacaoApp.Application.Common.Abstractions;

/// <summary>Entrega ao usuário o link de redefinição de senha (por e-mail, na implementação de produção).</summary>
public interface IPasswordResetNotifier
{
    Task NotifyAsync(PasswordResetTicket ticket, CancellationToken cancellationToken = default);
}
