using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Donations.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Donations;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Donations.Queries;

/// <summary>Lista as doações do ponto (mais recentes primeiro). Escopada ao ponto do gestor.</summary>
public sealed record ListPointDonationsQuery(Guid PointId)
    : IRequest<ErrorOr<IReadOnlyList<DonationResponse>>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class ListPointDonationsHandler(IDonationRepository repository)
    : IRequestHandler<ListPointDonationsQuery, ErrorOr<IReadOnlyList<DonationResponse>>>
{
    private const int MaxDonationsListed = 200;

    public async Task<ErrorOr<IReadOnlyList<DonationResponse>>> Handle(
        ListPointDonationsQuery query,
        CancellationToken cancellationToken)
    {
        var donations = await repository.ListByPointAsync(
            CollectionPointId.From(query.PointId), MaxDonationsListed, cancellationToken);

        IReadOnlyList<DonationResponse> response = [.. donations.Select(d => d.ToResponse())];

        return ErrorOrFactory.From(response);
    }
}
