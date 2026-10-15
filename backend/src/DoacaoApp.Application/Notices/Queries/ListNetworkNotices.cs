using DoacaoApp.Application.Notices.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Notices;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Notices.Queries;

/// <summary>
/// Mural da rede: avisos ativos (públicos e para gestores) de todos os pontos, mais recentes
/// primeiro. Área privada — o gestor acompanha o que os demais pontos comunicam.
/// </summary>
public sealed record ListNetworkNoticesQuery : IRequest<ErrorOr<IReadOnlyList<NoticeResponse>>>;

public sealed class ListNetworkNoticesHandler(INoticeRepository noticeRepository, ICollectionPointRepository pointRepository)
    : IRequestHandler<ListNetworkNoticesQuery, ErrorOr<IReadOnlyList<NoticeResponse>>>
{
    private const int MaxNoticesListed = 100;

    public async Task<ErrorOr<IReadOnlyList<NoticeResponse>>> Handle(
        ListNetworkNoticesQuery query,
        CancellationToken cancellationToken)
    {
        var notices = await noticeRepository.ListNetworkAsync(MaxNoticesListed, cancellationToken);
        var points = await pointRepository.ListAllAsync(cancellationToken);
        var pointNames = points.ToDictionary(point => point.Id, point => point.Name);

        IReadOnlyList<NoticeResponse> response =
            [.. notices.Select(n => n.ToResponse(pointNames.GetValueOrDefault(n.PointId)))];

        return ErrorOrFactory.From(response);
    }
}
