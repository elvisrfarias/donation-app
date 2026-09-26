using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Beneficiaries.Queries;

/// <summary>
/// Lista os beneficiários visíveis ao usuário: um Admin vê todos; um PointManager/Viewer só
/// vê os beneficiários cadastrados pelo seu próprio ponto de coleta.
/// </summary>
public sealed record ListBeneficiariesQuery : IRequest<ErrorOr<IReadOnlyList<BeneficiaryResponse>>>;

public sealed class ListBeneficiariesHandler(IBeneficiaryRepository repository, ICurrentUser currentUser)
    : IRequestHandler<ListBeneficiariesQuery, ErrorOr<IReadOnlyList<BeneficiaryResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<BeneficiaryResponse>>> Handle(
        ListBeneficiariesQuery query,
        CancellationToken cancellationToken)
    {
        var beneficiaries = currentUser.IsAdmin
            ? await repository.ListAllAsync(cancellationToken)
            : await repository.ListByCollectionPointAsync(
                CollectionPointId.From(currentUser.CollectionPointId ?? Guid.Empty), cancellationToken);

        IReadOnlyList<BeneficiaryResponse> response = beneficiaries.Select(b => b.ToResponse()).ToList();

        return ErrorOrFactory.From(response);
    }
}
