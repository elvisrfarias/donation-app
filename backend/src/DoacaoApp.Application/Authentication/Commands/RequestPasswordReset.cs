using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Authentication.Commands;

/// <summary>
/// "Esqueci minha senha": envia um link de redefinição ao e-mail informado, se houver conta ativa com ele. A
/// resposta é sempre a mesma, exista ou não a conta, para não revelar quais e-mails estão cadastrados.
/// </summary>
public sealed record RequestPasswordResetCommand(string Email) : IRequest<ErrorOr<Success>>;

public sealed class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class RequestPasswordResetHandler(IIdentityService identityService, IPasswordResetNotifier notifier)
    : IRequestHandler<RequestPasswordResetCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        var ticket = await identityService.CreatePasswordResetTicketAsync(command.Email, cancellationToken);
        if (ticket is not null)
        {
            await notifier.NotifyAsync(ticket, cancellationToken);
        }

        return Result.Success;
    }
}
