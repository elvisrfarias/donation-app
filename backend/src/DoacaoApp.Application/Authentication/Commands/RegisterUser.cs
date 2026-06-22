using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>Registra um novo usuário gerencial (operação restrita a administradores).</summary>
public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FullName,
    string Role,
    Guid? CollectionPointId) : IRequest<ErrorOr<AuthResult>>;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Role).NotEmpty().Must(Roles.IsValid).WithMessage("Role inválida.");
    }
}

public sealed class RegisterUserHandler(IIdentityService identityService)
    : IRequestHandler<RegisterUserCommand, ErrorOr<AuthResult>>
{
    public Task<ErrorOr<AuthResult>> Handle(RegisterUserCommand command, CancellationToken cancellationToken) =>
        identityService.RegisterAsync(
            new RegisterRequest(command.Email, command.Password, command.FullName, command.Role, command.CollectionPointId),
            cancellationToken);
}