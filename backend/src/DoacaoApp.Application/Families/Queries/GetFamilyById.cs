using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Families.Contracts;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Families;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Families.Queries;

/// <summary>Carrega uma família com seus beneficiários.</summary>
public sealed record GetFamilyByIdQuery(Guid FamilyId) : IRequest<ErrorOr<FamilyResponse>>;

public sealed class GetFamilyByIdHandler(IFamilyRepository repository, ICurrentUser currentUser)
    : IRequestHandler<GetFamilyByIdQuery, ErrorOr<FamilyResponse>>
{
    public async Task<ErrorOr<FamilyResponse>> Handle(
        GetFamilyByIdQuery query,
        CancellationToken cancellationToken)
    {
        var family = await repository.GetByIdAsync(FamilyId.From(query.FamilyId), cancellationToken);

        if (family is null || !currentUser.CanAccess(family))
        {
            return DomainErrors.Family.NotFound;
        }

        return family.ToResponse();
    }
}