using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Application.Beneficiaries.Queries;

/// <summary>
/// Busca rápida por CPF no município inteiro (o CPF é único no município). Usada no balcão para
/// localizar o beneficiário e, se ele ainda não for atendido pelo ponto, decidir vinculá-lo. Só quem
/// já tem acesso ao cadastro recebe os dados completos; os demais recebem uma projeção mascarada.
/// </summary>
public sealed record FindBeneficiaryByCpfQuery(string Cpf) : IRequest<ErrorOr<BeneficiaryCpfLookupResponse>>;

public sealed class FindBeneficiaryByCpfValidator : AbstractValidator<FindBeneficiaryByCpfQuery>
{
    public FindBeneficiaryByCpfValidator()
    {
        RuleFor(x => x.Cpf).NotEmpty().MaximumLength(20);
    }
}

public sealed class FindBeneficiaryByCpfHandler(
    IBeneficiaryRepository repository,
    ICurrentUser currentUser,
    ILogger<FindBeneficiaryByCpfHandler> logger)
    : IRequestHandler<FindBeneficiaryByCpfQuery, ErrorOr<BeneficiaryCpfLookupResponse>>
{
    public async Task<ErrorOr<BeneficiaryCpfLookupResponse>> Handle(
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

        if (currentUser.CanAccess(beneficiary))
        {
            return beneficiary.ToFullLookup(IsLinkedToCurrentUserPoint(beneficiary));
        }

        logger.LogInformation(
            "Auditoria: usuário {UserId} (ponto {PointId}) localizou por CPF o beneficiário {BeneficiaryId}, não vinculado ao seu ponto.",
            currentUser.UserId,
            currentUser.CollectionPointId,
            beneficiary.Id.Value);

        return beneficiary.ToMaskedLookup();
    }

    private bool IsLinkedToCurrentUserPoint(Beneficiary beneficiary) =>
        currentUser.CollectionPointId is { } pointId && beneficiary.IsLinkedTo(CollectionPointId.From(pointId));
}
