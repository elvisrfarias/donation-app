using DoacaoApp.Domain.Notices;

namespace DoacaoApp.Application.Notices.Contracts;

/// <summary>Aviso do mural de um ponto de coleta.</summary>
public sealed record NoticeResponse(
    Guid Id,
    Guid CollectionPointId,
    string? PointName,
    string Title,
    string Message,
    string Audience,
    string AuthorName,
    DateTime CreatedAtUtc);

public static class NoticeMappings
{
    public static NoticeResponse ToResponse(this PointNotice notice, string? pointName = null) =>
        new(
            notice.Id.Value,
            notice.PointId.Value,
            pointName,
            notice.Title,
            notice.Message,
            notice.Audience.ToString(),
            notice.AuthorName,
            notice.CreatedAtUtc);
}
