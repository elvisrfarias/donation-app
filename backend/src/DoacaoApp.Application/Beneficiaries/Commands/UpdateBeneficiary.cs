using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Beneficiaries.Commands;

/// <summary>
/// Atualiza os dados do beneficiário (nome, CPF, nascimento e ponto de cadastro). Um gestor de
/// ponto só edita beneficiários cadastrados pelo seu ponto.
/// </summary>
public sealed record UpdateBeneficiaryCommand(
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
        RuleFor(x => x.BeneficiaryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cpf).NotEmpty();
        RuleFor(x => x.RegisteredByPointId).NotEmpty();
    }
}

public sealed class UpdateBeneficiaryHandler(
    IBeneficiaryRepository beneficiaryRepository,
    ICollectionPointRepository pointRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
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

        var beneficiary = await beneficiaryRepository.GetByIdAsync(
            BeneficiaryId.From(command.BeneficiaryId), cancellationToken);
        if (beneficiary is null || !currentUser.CanAccess(beneficiary))
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        var point = await pointRepository.GetByIdAsync(
            CollectionPointId.From(command.RegisteredByPointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        if (await beneficiaryRepository.ExistsWithCpfAsync(cpf.Value, beneficiary.Id, cancellationToken))
        {
            return DomainErrors.Beneficiary.CpfAlreadyRegistered;
        }

        var updated = beneficiary.UpdateDetails(command.Name, cpf.Value, command.BirthDate, point.Id);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return beneficiary.ToResponse();
    }
}
