using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Infrastructure.Mailing;

/// <summary>
/// Enfileira o e-mail com o link de redefinição. O token vai no fragmento da URL (depois do "#"), que o navegador
/// nunca envia ao servidor: assim ele não fica em logs de proxy/CDN nem no cabeçalho Referer.
/// </summary>
public sealed class PasswordResetEmailNotifier(
    EmailOutbox outbox,
    IOptions<EmailOptions> emailOptions,
    IOptions<SecurityOptions> securityOptions) : IPasswordResetNotifier
{
    public const string ResetPagePath = "reset-password";

    public Task NotifyAsync(PasswordResetTicket ticket, CancellationToken cancellationToken = default)
    {
        var message = PasswordResetEmail.Build(
            ticket,
            BuildResetLink(emailOptions.Value.AppBaseUrl, ticket.Token),
            securityOptions.Value.PasswordResetTokenMinutes);

        return outbox.EnqueueAsync(message, cancellationToken).AsTask();
    }

    public static Uri BuildResetLink(string appBaseUrl, string token) =>
        new($"{appBaseUrl.TrimEnd('/')}/{ResetPagePath}#token={Uri.EscapeDataString(token)}");
}
