using DoacaoApp.Application.Common.Abstractions;
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
/// Caso de uso "vincular beneficiário à família" (LinkBeneficiaryToFamily).
/// Um ponto cadastra o beneficiário (CPF), garante unicidade municipal e o vincula à família.
/// </summary>
public sealed record RegisterBeneficiaryCommand(
    Guid FamilyId,
    string Name,
    string Cpf,
    DateOnly? BirthDate,
    Guid RegisteredByPointId) : IRequest<ErrorOr<BeneficiaryResponse>>;

public sealed class RegisterBeneficiaryValidator : AbstractValidator<RegisterBeneficiaryCommand>
{
    public RegisterBeneficiaryValidator()
    {
        RuleFor(x => x.FamilyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cpf).NotEmpty();
        RuleFor(x => x.RegisteredByPointId).NotEmpty();
    }
}

public sealed class RegisterBeneficiaryHandler(
    IFamilyRepository familyRepository,
    ICollectionPointRepository pointRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<RegisterBeneficiaryCommand, ErrorOr<BeneficiaryResponse>>
{
    public async Task<ErrorOr<BeneficiaryResponse>> Handle(
        RegisterBeneficiaryCommand command,
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

        if (await familyRepository.ExistsBeneficiaryWithCpfAsync(cpf.Value, cancellationToken))
        {
            return DomainErrors.Beneficiary.CpfAlreadyRegistered;
        }

        var beneficiary = family.AddBeneficiary(
            command.Name, cpf.Value, command.BirthDate, point.Id, clock.UtcNow);

        if (beneficiary.IsError)
        {
            return beneficiary.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return beneficiary.Value.ToResponse();
    }
}