using DoacaoApp.Application.Families.Contracts;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Families.Queries;

/// <summary>
/// Busca rápida por CPF: retorna a família (com seus beneficiários) dona do CPF informado.
/// Usada no registro de retirada para preencher família/beneficiário sem digitar IDs.
/// </summary>
public sealed record FindFamilyByCpfQuery(string Cpf) : IRequest<ErrorOr<FamilyResponse>>;

public sealed class FindFamilyByCpfHandler(IFamilyRepository repository)
    : IRequestHandler<FindFamilyByCpfQuery, ErrorOr<FamilyResponse>>
{
    public async Task<ErrorOr<FamilyResponse>> Handle(
        FindFamilyByCpfQuery query,
        CancellationToken cancellationToken)
    {
        var cpf = Cpf.Create(query.Cpf);
        if (cpf.IsError)
        {
            return cpf.Errors;
        }

        var family = await repository.GetByBeneficiaryCpfAsync(cpf.Value, cancellationToken);
        if (family is null)
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        return family.ToResponse();
    }
}
