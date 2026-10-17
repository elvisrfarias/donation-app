using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>
/// Redefine a senha de um usuário e encerra todas as sessões dele. Admin redefine a de qualquer usuário; um
/// PointManager só a de PointManager/Viewer do seu próprio ponto (nunca a de um Admin).
/// </summary>
public sealed record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest<ErrorOr<Success>>;

public sealed class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPassword).MustSatisfyPasswordPolicy();
    }
}

public sealed class ResetUserPasswordHandler(IIdentityService identityService, ICurrentUser currentUser)
    : IRequestHandler<ResetUserPasswordCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(ResetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin)
        {
            var existingUser = await identityService.GetUserByIdAsync(command.UserId, cancellationToken);
            if (existingUser.IsError)
            {
                return existingUser.Errors;
            }

            if (!IsManagedByCurrentPointManager(existingUser.Value))
            {
                return ScopeErrors.CollectionPointForbidden;
            }
        }

        return await identityService.ResetPasswordAsync(command.UserId, command.NewPassword, cancellationToken);
    }

    private bool IsManagedByCurrentPointManager(UserResponse user) =>
        currentUser.CollectionPointId is { } ownPointId
        && user.CollectionPointId == ownPointId
        && !user.Roles.Contains(Roles.Admin);
}
