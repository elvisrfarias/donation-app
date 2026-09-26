using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Beneficiaries.Queries;

/// <summary>Carrega um beneficiário (respeitando a visibilidade por ponto de coleta).</summary>
public sealed record GetBeneficiaryByIdQuery(Guid BeneficiaryId) : IRequest<ErrorOr<BeneficiaryResponse>>;

public sealed class GetBeneficiaryByIdHandler(IBeneficiaryRepository repository, ICurrentUser currentUser)
    : IRequestHandler<GetBeneficiaryByIdQuery, ErrorOr<BeneficiaryResponse>>
{
    public async Task<ErrorOr<BeneficiaryResponse>> Handle(
        GetBeneficiaryByIdQuery query,
        CancellationToken cancellationToken)
    {
        var beneficiary = await repository.GetByIdAsync(BeneficiaryId.From(query.BeneficiaryId), cancellationToken);

        if (beneficiary is null || !currentUser.CanAccess(beneficiary))
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        return beneficiary.ToResponse();
    }
}
