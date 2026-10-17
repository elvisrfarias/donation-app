using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>
/// Registra um novo usuário gerencial. Admin cria qualquer papel em qualquer ponto; um PointManager só
/// pode criar PointManager/Viewer no seu próprio ponto de coleta. Devolve só o usuário criado: os tokens
/// de sessão nunca são entregues a quem cadastrou.
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FullName,
    string Role,
    Guid? CollectionPointId) : IRequest<ErrorOr<UserResponse>>;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MustSatisfyPasswordPolicy();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Role).NotEmpty().Must(Roles.IsValid).WithMessage("Role inválida.");

        RuleFor(x => x.CollectionPointId)
            .Null()
            .When(x => x.Role == Roles.Admin)
            .WithMessage("Um Admin não deve estar vinculado a um ponto de coleta.");

        RuleFor(x => x.CollectionPointId)
            .NotNull()
            .When(x => x.Role is Roles.PointManager or Roles.Viewer)
            .WithMessage("Informe o ponto de coleta ao qual este usuário pertence.");
    }
}

public sealed class RegisterUserHandler(IIdentityService identityService, ICurrentUser currentUser)
    : IRequestHandler<RegisterUserCommand, ErrorOr<UserResponse>>
{
    public Task<ErrorOr<UserResponse>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin)
        {
            if (command.Role is not (Roles.PointManager or Roles.Viewer))
            {
                return Task.FromResult<ErrorOr<UserResponse>>(AuthErrors.InsufficientPermissionsToAssignRole);
            }

            if (command.CollectionPointId != currentUser.CollectionPointId)
            {
                return Task.FromResult<ErrorOr<UserResponse>>(ScopeErrors.CollectionPointForbidden);
            }
        }

        return identityService.RegisterAsync(
            new RegisterRequest(command.Email, command.Password, command.FullName, command.Role, command.CollectionPointId),
            cancellationToken);
    }
}