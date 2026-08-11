using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>Desativa um usuário gerencial, bloqueando novos logins (operação restrita a administradores).</summary>
public sealed record DeactivateUserCommand(Guid UserId) : IRequest<ErrorOr<Success>>;

public sealed class DeactivateUserValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class DeactivateUserHandler(IIdentityService identityService)
    : IRequestHandler<DeactivateUserCommand, ErrorOr<Success>>
{
    public Task<ErrorOr<Success>> Handle(DeactivateUserCommand command, CancellationToken cancellationToken) =>
        identityService.DeactivateUserAsync(command.UserId, cancellationToken);
}
