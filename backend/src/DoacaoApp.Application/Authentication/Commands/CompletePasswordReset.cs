using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>Define a nova senha a partir do token recebido no link de "esqueci minha senha".</summary>
public sealed record CompletePasswordResetCommand(string Token, string NewPassword) : IRequest<ErrorOr<Success>>;

public sealed class CompletePasswordResetValidator : AbstractValidator<CompletePasswordResetCommand>
{
    public CompletePasswordResetValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).MustSatisfyPasswordPolicy();
    }
}

public sealed class CompletePasswordResetHandler(IIdentityService identityService)
    : IRequestHandler<CompletePasswordResetCommand, ErrorOr<Success>>
{
    public Task<ErrorOr<Success>> Handle(CompletePasswordResetCommand command, CancellationToken cancellationToken) =>
        identityService.ResetForgottenPasswordAsync(command.Token, command.NewPassword, cancellationToken);
}
