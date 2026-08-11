using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Families.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Families.Commands;

/// <summary>
/// Caso de uso "atualizar dados do beneficiário" (nome, CPF, nascimento e ponto de cadastro).
/// </summary>
public sealed record UpdateBeneficiaryCommand(
    Guid FamilyId,
    Guid BeneficiaryId,
    string Name,
    string Cpf,
    DateOnly? BirthDate,
    Guid RegisteredByPointId) : IRequest<ErrorOr<BeneficiaryResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => RegisteredByPointId;
}

public sealed class UpdateBeneficiaryValidator : AbstractValidator<UpdateBeneficiaryCommand>
{
    public UpdateBeneficiaryValidator()
    {
        RuleFor(x => x.FamilyId).NotEmpty();
        RuleFor(x => x.BeneficiaryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cpf).NotEmpty();
        RuleFor(x => x.RegisteredByPointId).NotEmpty();
    }
}

public sealed class UpdateBeneficiaryHandler(
    IFamilyRepository familyRepository,
    ICollectionPointRepository pointRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBeneficiaryCommand, ErrorOr<BeneficiaryResponse>>
{
    public async Task<ErrorOr<BeneficiaryResponse>> Handle(
        UpdateBeneficiaryCommand command,
        CancellationToken cancellationToken)
    {
        var cpf = Cpf.Create(command.Cpf);
        if (cpf.IsError)
        {
            return cpf.Errors;
        }

        var family = await familyRepository.GetByIdAsync(FamilyId.From(command.FamilyId), cancellationToken);
        if (family is null)
        {
            return DomainErrors.Family.NotFound;
        }

        var point = await pointRepository.GetByIdAsync(
            CollectionPointId.From(command.RegisteredByPointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var beneficiaryId = BeneficiaryId.From(command.BeneficiaryId);

        if (await familyRepository.ExistsBeneficiaryWithCpfAsync(cpf.Value, beneficiaryId, cancellationToken))
        {
            return DomainErrors.Beneficiary.CpfAlreadyRegistered;
        }

        var beneficiary = family.UpdateBeneficiary(
            beneficiaryId, command.Name, cpf.Value, command.BirthDate, point.Id);

        if (beneficiary.IsError)
        {
            return beneficiary.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return beneficiary.Value.ToResponse();
    }
}
