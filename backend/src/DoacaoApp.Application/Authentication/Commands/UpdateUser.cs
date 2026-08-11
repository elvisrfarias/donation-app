using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>Atualiza nome, papel e ponto de coleta de um usuário existente (operação restrita a administradores).</summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string FullName,
    string Role,
    Guid? CollectionPointId) : IRequest<ErrorOr<UserResponse>>;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
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

public sealed class UpdateUserHandler(IIdentityService identityService)
    : IRequestHandler<UpdateUserCommand, ErrorOr<UserResponse>>
{
    public Task<ErrorOr<UserResponse>> Handle(UpdateUserCommand command, CancellationToken cancellationToken) =>
        identityService.UpdateUserAsync(
            new UpdateUserRequest(command.UserId, command.FullName, command.Role, command.CollectionPointId),
            cancellationToken);
}
