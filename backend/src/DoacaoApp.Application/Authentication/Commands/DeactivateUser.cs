using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>
/// Desativa um usuário gerencial, bloqueando novos logins. Admin desativa qualquer usuário; um
/// PointManager só desativa PointManager/Viewer do seu próprio ponto.
/// </summary>
public sealed record DeactivateUserCommand(Guid UserId) : IRequest<ErrorOr<Success>>;

public sealed class DeactivateUserValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class DeactivateUserHandler(IIdentityService identityService, ICurrentUser currentUser)
    : IRequestHandler<DeactivateUserCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin)
        {
            var existingUser = await identityService.GetUserByIdAsync(command.UserId, cancellationToken);
            if (existingUser.IsError)
            {
                return existingUser.Errors;
            }

            if (existingUser.Value.CollectionPointId != currentUser.CollectionPointId)
            {
                return ScopeErrors.CollectionPointForbidden;
            }
        }

        return await identityService.DeactivateUserAsync(command.UserId, cancellationToken);
    }
}
