using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;

namespace DoacaoApp.Domain.Notices;

/// <summary>
/// Aviso publicado por um ponto de coleta no mural — Aggregate Root. É o canal de comunicação do
/// ponto: avisos públicos chegam à comunidade (ex.: "precisamos de leite em pó") e avisos para
/// gestores circulam só entre os pontos da rede. Referencia o ponto por Id (outro agregado).
/// </summary>
public sealed class PointNotice : AggregateRoot<NoticeId>
{
    public const int MaxTitleLength = 120;
    public const int MaxMessageLength = 1000;
    public const int MaxAuthorNameLength = 120;

    private PointNotice()
    {
        Title = string.Empty;
        Message = string.Empty;
        AuthorName = string.Empty;
    }

    private PointNotice(
        NoticeId id,
        CollectionPointId pointId,
        string title,
        string message,
        NoticeAudience audience,
        string authorName,
        DateTime createdAtUtc) : base(id)
    {
        PointId = pointId;
        Title = title;
        Message = message;
        Audience = audience;
        AuthorName = authorName;
        CreatedAtUtc = createdAtUtc;
    }

    public CollectionPointId PointId { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public NoticeAudience Audience { get; private set; }
    public string AuthorName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsArchived { get; private set; }

    public static ErrorOr<PointNotice> Publish(
        CollectionPointId pointId,
        string? title,
        string? message,
        NoticeAudience audience,
        string? authorName,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return DomainErrors.Notice.TitleRequired;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return DomainErrors.Notice.MessageRequired;
        }

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > MaxTitleLength)
        {
            return DomainErrors.Notice.TitleTooLong(MaxTitleLength);
        }

        var trimmedMessage = message.Trim();
        if (trimmedMessage.Length > MaxMessageLength)
        {
            return DomainErrors.Notice.MessageTooLong(MaxMessageLength);
        }

        var signature = string.IsNullOrWhiteSpace(authorName) ? "Equipe do ponto" : authorName.Trim();

        return new PointNotice(
            NoticeId.New(), pointId, trimmedTitle, trimmedMessage, audience, signature, createdAtUtc);
    }

    public void Archive() => IsArchived = true;
}
