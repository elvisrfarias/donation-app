using DoacaoApp.Application.Families.Contracts;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Families;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Families.Queries;

/// <summary>Carrega uma família com seus beneficiários.</summary>
public sealed record GetFamilyByIdQuery(Guid FamilyId) : IRequest<ErrorOr<FamilyResponse>>;

public sealed class GetFamilyByIdHandler(IFamilyRepository repository)
    : IRequestHandler<GetFamilyByIdQuery, ErrorOr<FamilyResponse>>
{
    public async Task<ErrorOr<FamilyResponse>> Handle(
        GetFamilyByIdQuery query,
        CancellationToken cancellationToken)
    {
        var family = await repository.GetByIdAsync(FamilyId.From(query.FamilyId), cancellationToken);

        return family is null
            ? DomainErrors.Family.NotFound
            : family.ToResponse();
    }
}