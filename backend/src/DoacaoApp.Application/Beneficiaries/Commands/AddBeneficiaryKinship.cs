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
/// Registra um vínculo de parentesco de um beneficiário com outro (localizado pelo CPF).
/// O vínculo é sempre espelhado no outro beneficiário com o tipo inverso. Os vínculos são
/// informativos: não alteram a cota de retiradas de nenhum dos dois. Um gestor só registra vínculos
/// de beneficiários vinculados ao seu ponto.
/// </summary>
public sealed record AddBeneficiaryKinshipCommand(
    Guid BeneficiaryId,
    string RelatedBeneficiaryCpf,
    KinshipType Type) : IRequest<ErrorOr<BeneficiaryResponse>>;

public sealed class AddBeneficiaryKinshipValidator : AbstractValidator<AddBeneficiaryKinshipCommand>
{
    public AddBeneficiaryKinshipValidator()
    {
        RuleFor(x => x.BeneficiaryId).NotEmpty();
        RuleFor(x => x.RelatedBeneficiaryCpf).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
    }
}

public sealed class AddBeneficiaryKinshipHandler(
    IBeneficiaryRepository repository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ICurrentUser currentUser)
    : IRequestHandler<AddBeneficiaryKinshipCommand, ErrorOr<BeneficiaryResponse>>
{
    public async Task<ErrorOr<BeneficiaryResponse>> Handle(
        AddBeneficiaryKinshipCommand command,
        CancellationToken cancellationToken)
    {
        var subject = await repository.GetByIdAsync(BeneficiaryId.From(command.BeneficiaryId), cancellationToken);
        if (subject is null || !currentUser.CanAccess(subject))
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        var cpf = Cpf.Create(command.RelatedBeneficiaryCpf);
        if (cpf.IsError)
        {
            return cpf.Errors;
        }

        var related = await repository.GetByCpfAsync(cpf.Value, cancellationToken);
        if (related is null)
        {
            return DomainErrors.Beneficiary.RelatedBeneficiaryNotFound;
        }

        var now = clock.UtcNow;

        var forward = subject.AddKinship(related.Id, related.Name, related.Cpf, command.Type, now);
        if (forward.IsError)
        {
            return forward.Errors;
        }

        var backward = related.AddKinship(subject.Id, subject.Name, subject.Cpf, command.Type.Inverse(), now);
        if (backward.IsError)
        {
            return backward.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return subject.ToResponse();
    }
}
