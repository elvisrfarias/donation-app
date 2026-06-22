using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>Troca um refresh token válido por um novo par de tokens (rotação).</summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<ErrorOr<AuthResult>>;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RefreshTokenHandler(IIdentityService identityService)
    : IRequestHandler<RefreshTokenCommand, ErrorOr<AuthResult>>
{
    public Task<ErrorOr<AuthResult>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken) =>
        identityService.RefreshAsync(command.RefreshToken, cancellationToken);
}

/// <summary>Revoga um refresh token (logout).</summary>
public sealed record RevokeTokenCommand(string RefreshToken) : IRequest<ErrorOr<Success>>;

public sealed class RevokeTokenValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RevokeTokenHandler(IIdentityService identityService)
    : IRequestHandler<RevokeTokenCommand, ErrorOr<Success>>
{
    public Task<ErrorOr<Success>> Handle(RevokeTokenCommand command, CancellationToken cancellationToken) =>
        identityService.RevokeAsync(command.RefreshToken, cancellationToken);
}
