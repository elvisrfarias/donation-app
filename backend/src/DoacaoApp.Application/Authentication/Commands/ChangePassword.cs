using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>
/// Troca a senha do usuário autenticado. As demais sessões dele são encerradas e a resposta traz um novo
/// par de tokens, que substitui o da sessão corrente.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<ErrorOr<AuthResult>>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Informe a senha atual.");
        RuleFor(x => x.NewPassword).MustSatisfyPasswordPolicy();

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("A nova senha deve ser diferente da atual.");
    }
}

public sealed class ChangePasswordHandler(IIdentityService identityService, ICurrentUser currentUser)
    : IRequestHandler<ChangePasswordCommand, ErrorOr<AuthResult>>
{
    public async Task<ErrorOr<AuthResult>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AuthErrors.NotAuthenticated;
        }

        return await identityService.ChangePasswordAsync(
            new ChangePasswordRequest(userId, command.CurrentPassword, command.NewPassword),
            cancellationToken);
    }
}
