using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Beneficiaries.Commands;

/// <summary>
/// Atualiza os dados do beneficiário (nome, CPF e nascimento). Os dados são da pessoa e valem para
/// todos os pontos que a atendem; um gestor só edita beneficiários vinculados ao seu ponto.
/// </summary>
public sealed record UpdateBeneficiaryCommand(
    Guid BeneficiaryId,
    string Name,
    string Cpf,
    DateOnly? BirthDate) : IRequest<ErrorOr<BeneficiaryResponse>>;

public sealed class UpdateBeneficiaryValidator : AbstractValidator<UpdateBeneficiaryCommand>
{
    public UpdateBeneficiaryValidator()
    {
        RuleFor(x => x.BeneficiaryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cpf).NotEmpty();
    }
}

public sealed class UpdateBeneficiaryHandler(
    IBeneficiaryRepository beneficiaryRepository,
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

        if (await beneficiaryRepository.ExistsWithCpfAsync(cpf.Value, beneficiary.Id, cancellationToken))
        {
            return DomainErrors.Beneficiary.CpfAlreadyRegistered;
        }

        var updated = beneficiary.UpdateDetails(command.Name, cpf.Value, command.BirthDate);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return beneficiary.ToResponse();
    }
}
