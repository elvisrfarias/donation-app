namespace DoacaoApp.Infrastructure.Mailing;

/// <summary>Transporte que entrega de fato um e-mail ao provedor.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
