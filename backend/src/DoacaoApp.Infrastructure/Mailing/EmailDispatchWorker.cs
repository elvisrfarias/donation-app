using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Infrastructure.Mailing;

/// <summary>Esvazia a <see cref="EmailOutbox"/> em segundo plano; a falha de um envio só é registrada no log.</summary>
public sealed class EmailDispatchWorker(
    EmailOutbox outbox,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in outbox.ReadAllAsync(stoppingToken))
        {
            await DispatchAsync(message, stoppingToken);
        }
    }

    private async Task DispatchAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            await sender.SendAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falha ao enviar o e-mail \"{Subject}\" para {To}.", message.Subject, message.To);
        }
    }
}
