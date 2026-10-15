using DoacaoApp.Application.Notices.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Notices;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Notices.Queries;

/// <summary>Lista os avisos públicos ativos de um ponto (mais recentes primeiro). Público.</summary>
public sealed record ListPublicNoticesQuery(Guid CollectionPointId)
    : IRequest<ErrorOr<IReadOnlyList<NoticeResponse>>>;

public sealed class ListPublicNoticesHandler(INoticeRepository repository)
    : IRequestHandler<ListPublicNoticesQuery, ErrorOr<IReadOnlyList<NoticeResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<NoticeResponse>>> Handle(
        ListPublicNoticesQuery query,
        CancellationToken cancellationToken)
    {
        var notices = await repository.ListPublicByPointAsync(
            CollectionPointId.From(query.CollectionPointId), cancellationToken);

        IReadOnlyList<NoticeResponse> response = [.. notices.Select(n => n.ToResponse())];

        return ErrorOrFactory.From(response);
    }
}
