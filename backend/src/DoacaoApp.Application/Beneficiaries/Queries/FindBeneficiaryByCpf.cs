using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Beneficiaries.Queries;

/// <summary>
/// Busca rápida por CPF no município inteiro (o histórico de retiradas é compartilhado entre
/// os pontos). Usada no registro de retirada para localizar o beneficiário sem digitar IDs.
/// </summary>
public sealed record FindBeneficiaryByCpfQuery(string Cpf) : IRequest<ErrorOr<BeneficiaryResponse>>;

public sealed class FindBeneficiaryByCpfHandler(IBeneficiaryRepository repository)
    : IRequestHandler<FindBeneficiaryByCpfQuery, ErrorOr<BeneficiaryResponse>>
{
    public async Task<ErrorOr<BeneficiaryResponse>> Handle(
        FindBeneficiaryByCpfQuery query,
        CancellationToken cancellationToken)
    {
        var cpf = Cpf.Create(query.Cpf);
        if (cpf.IsError)
        {
            return cpf.Errors;
        }

        var beneficiary = await repository.GetByCpfAsync(cpf.Value, cancellationToken);
        if (beneficiary is null)
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        return beneficiary.ToResponse();
    }
}
