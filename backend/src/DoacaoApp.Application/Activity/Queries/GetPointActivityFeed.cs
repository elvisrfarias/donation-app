using DoacaoApp.Application.Activity.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Donations;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Notices;
using DoacaoApp.Domain.Reviews;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Activity.Queries;

/// <summary>
/// Feed de percepção do ponto: reúne num só lugar o que mudou recentemente — doações
/// prometidas, avisos, avaliações da comunidade e itens de estoque abaixo do limite de aviso.
/// Escopada ao ponto do gestor.
/// </summary>
public sealed record GetPointActivityFeedQuery(Guid PointId)
    : IRequest<ErrorOr<ActivityFeedResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class GetPointActivityFeedHandler(
    ICollectionPointRepository pointRepository,
    IDonationRepository donationRepository,
    INoticeRepository noticeRepository,
    IReviewRepository reviewRepository,
    IDateTimeProvider clock)
    : IRequestHandler<GetPointActivityFeedQuery, ErrorOr<ActivityFeedResponse>>
{
    private const int ItemsPerSource = 10;
    private const int MaxFeedItems = 20;

    public async Task<ErrorOr<ActivityFeedResponse>> Handle(
        GetPointActivityFeedQuery query,
        CancellationToken cancellationToken)
    {
        var pointId = CollectionPointId.From(query.PointId);
        var point = await pointRepository.GetByIdAsync(pointId, cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var donations = await donationRepository.ListByPointAsync(pointId, ItemsPerSource, cancellationToken);
        var notices = await noticeRepository.ListByPointAsync(pointId, ItemsPerSource, cancellationToken);
        var reviews = await reviewRepository.ListByPointAsync(pointId, cancellationToken);

        IReadOnlyList<ActivityItemResponse> items =
        [
            .. LowStockAlerts(point),
            .. donations.Select(ToActivity),
            .. notices.Select(ToActivity),
            .. reviews.Take(ItemsPerSource).Select(ToActivity),
        ];

        return new ActivityFeedResponse(
            [.. items.OrderByDescending(item => item.OccurredAtUtc).Take(MaxFeedItems)]);
    }

    private IEnumerable<ActivityItemResponse> LowStockAlerts(CollectionPoint point) =>
        point.StockItems
            .Where(item => item.IsTracked && item.AlertThreshold > 0 && item.Quantity <= item.AlertThreshold)
            .Select(item => new ActivityItemResponse(
                ActivityKinds.LowStock,
                $"Estoque baixo: {item.Name}",
                $"Restam {item.Quantity} {item.Unit} (aviso em {item.AlertThreshold}).",
                clock.UtcNow));

    private static ActivityItemResponse ToActivity(Donation donation) =>
        new(
            ActivityKinds.Donation,
            $"Doação de {donation.DonorName}",
            $"{donation.Description} — {DescribeStatus(donation.Status)}.",
            donation.CreatedAtUtc);

    private static ActivityItemResponse ToActivity(PointNotice notice) =>
        new(ActivityKinds.Notice, notice.Title, notice.Message, notice.CreatedAtUtc);

    private static ActivityItemResponse ToActivity(Review review) =>
        new(
            ActivityKinds.Review,
            $"Nova avaliação de {review.AuthorName}: {review.Rating}/{Review.MaxRating}",
            review.Comment ?? "Sem comentário.",
            review.CreatedAtUtc);

    private static string DescribeStatus(DonationStatus status) => status switch
    {
        DonationStatus.Pledged => "aguardando entrega",
        DonationStatus.Received => "recebida",
        _ => "cancelada",
    };
}
