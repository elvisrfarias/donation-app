using DoacaoApp.Application.Families.Contracts;
using DoacaoApp.Domain.Families;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Families.Queries;

/// <summary>Lista todas as famílias com seus beneficiários.</summary>
public sealed record ListFamiliesQuery : IRequest<ErrorOr<IReadOnlyList<FamilyResponse>>>;

public sealed class ListFamiliesHandler(IFamilyRepository repository)
    : IRequestHandler<ListFamiliesQuery, ErrorOr<IReadOnlyList<FamilyResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<FamilyResponse>>> Handle(
        ListFamiliesQuery query,
        CancellationToken cancellationToken)
    {
        var families = await repository.ListAllAsync(cancellationToken);

        IReadOnlyList<FamilyResponse> response = families.Select(f => f.ToResponse()).ToList();

        return ErrorOrFactory.From(response);
    }
}
