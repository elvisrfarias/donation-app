using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Families.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Families.Queries;

/// <summary>
/// Lista as famílias visíveis ao usuário: um Admin vê todas; um PointManager/Viewer só vê
/// famílias com ao menos um beneficiário cadastrado pelo seu próprio ponto de coleta.
/// </summary>
public sealed record ListFamiliesQuery : IRequest<ErrorOr<IReadOnlyList<FamilyResponse>>>;

public sealed class ListFamiliesHandler(IFamilyRepository repository, ICurrentUser currentUser)
    : IRequestHandler<ListFamiliesQuery, ErrorOr<IReadOnlyList<FamilyResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<FamilyResponse>>> Handle(
        ListFamiliesQuery query,
        CancellationToken cancellationToken)
    {
        var families = currentUser.IsAdmin
            ? await repository.ListAllAsync(cancellationToken)
            : await repository.ListByCollectionPointAsync(
                CollectionPointId.From(currentUser.CollectionPointId ?? Guid.Empty), cancellationToken);

        IReadOnlyList<FamilyResponse> response = families.Select(f => f.ToResponse()).ToList();

        return ErrorOrFactory.From(response);
    }
}
