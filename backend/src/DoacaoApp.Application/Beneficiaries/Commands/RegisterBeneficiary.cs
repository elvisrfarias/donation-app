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
/// Cadastra um beneficiário: o ponto informa o CPF e o sistema garante a unicidade municipal.
/// O limite de retiradas é regra do ponto onde a retirada acontece, não do beneficiário.
/// </summary>
public sealed record RegisterBeneficiaryCommand(
    string Name,
    string Cpf,
    DateOnly? BirthDate,
    Guid RegisteredByPointId) : IRequest<ErrorOr<BeneficiaryResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => RegisteredByPointId;
}

public sealed class RegisterBeneficiaryValidator : AbstractValidator<RegisterBeneficiaryCommand>
{
    public RegisterBeneficiaryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cpf).NotEmpty();
        RuleFor(x => x.RegisteredByPointId).NotEmpty();
    }
}

public sealed class RegisterBeneficiaryHandler(
    IBeneficiaryRepository beneficiaryRepository,
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

        var point = await pointRepository.GetByIdAsync(
            CollectionPointId.From(command.RegisteredByPointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        if (await beneficiaryRepository.ExistsWithCpfAsync(cpf.Value, cancellationToken))
        {
            return DomainErrors.Beneficiary.CpfAlreadyRegistered;
        }

        var beneficiary = Beneficiary.Create(
            command.Name, cpf.Value, command.BirthDate, point.Id, clock.UtcNow);

        if (beneficiary.IsError)
        {
            return beneficiary.Errors;
        }

        beneficiaryRepository.Add(beneficiary.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return beneficiary.Value.ToResponse();
    }
}
