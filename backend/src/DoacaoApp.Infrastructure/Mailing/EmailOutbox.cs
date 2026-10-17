using System.Threading.Channels;

namespace DoacaoApp.Infrastructure.Mailing;

/// <summary>
/// Fila em memória dos e-mails a enviar. Quem pede o envio só enfileira e segue, então o tempo de resposta não
/// depende do provedor nem revela se o e-mail foi de fato disparado.
/// </summary>
public sealed class EmailOutbox
{
    private const int Capacity = 500;

    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(Capacity) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
