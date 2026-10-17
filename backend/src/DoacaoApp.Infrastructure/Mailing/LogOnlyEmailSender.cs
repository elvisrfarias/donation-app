using Microsoft.Extensions.Logging;

namespace DoacaoApp.Infrastructure.Mailing;

/// <summary>
/// Usado quando o Resend não está configurado (desenvolvimento, testes): registra que o e-mail não saiu. O corpo,
/// que pode conter um link de redefinição de senha, só aparece no nível Debug.
/// </summary>
public sealed class LogOnlyEmailSender(ILogger<LogOnlyEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "E-mail \"{Subject}\" para {To} não enviado: Email:ResendApiKey não configurada.",
            message.Subject,
            message.To);
        logger.LogDebug("Corpo do e-mail não enviado:\n{TextBody}", message.TextBody);

        return Task.CompletedTask;
    }
}
