using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>Autentica por e-mail/senha e devolve access token + refresh token.</summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<ErrorOr<AuthResult>>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginHandler(IIdentityService identityService)
    : IRequestHandler<LoginCommand, ErrorOr<AuthResult>>
{
    public Task<ErrorOr<AuthResult>> Handle(LoginCommand command, CancellationToken cancellationToken) =>
        identityService.LoginAsync(command.Email, command.Password, cancellationToken);
}