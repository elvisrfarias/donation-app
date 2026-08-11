using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Families.Contracts;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Families.Commands;

/// <summary>
/// Registra um vínculo de parentesco de um beneficiário com outro (localizado pelo CPF).
/// O vínculo é sempre espelhado no outro beneficiário com o tipo inverso. Só
/// <see cref="KinshipType.Conjuge"/> entre beneficiários de famílias diferentes forma uma
/// família nova (com a cota do mais antigo dos dois); os demais tipos são informativos e
/// não alteram família nem cota de nenhum dos dois lados.
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
    IFamilyRepository familyRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<AddBeneficiaryKinshipCommand, ErrorOr<BeneficiaryResponse>>
{
    public async Task<ErrorOr<BeneficiaryResponse>> Handle(
        AddBeneficiaryKinshipCommand command,
        CancellationToken cancellationToken)
    {
        var subjectId = BeneficiaryId.From(command.BeneficiaryId);
        var subjectFamily = await familyRepository.GetByBeneficiaryIdAsync(subjectId, cancellationToken);
        if (subjectFamily is null)
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        var cpf = Cpf.Create(command.RelatedBeneficiaryCpf);
        if (cpf.IsError)
        {
            return cpf.Errors;
        }

        var relatedFamily = await familyRepository.GetByBeneficiaryCpfAsync(cpf.Value, cancellationToken);
        if (relatedFamily is null)
        {
            return DomainErrors.Beneficiary.RelatedBeneficiaryNotFound;
        }

        var subject = subjectFamily.FindBeneficiary(subjectId)!;
        var related = relatedFamily.Beneficiaries.First(b => b.Cpf == cpf.Value);
        var now = clock.UtcNow;

        Family subjectSideFamily;
        Family relatedSideFamily;

        if (subjectFamily.Id == relatedFamily.Id)
        {
            subjectSideFamily = subjectFamily;
            relatedSideFamily = subjectFamily;
        }
        else if (command.Type == KinshipType.Conjuge)
        {
            var newFamily = FormNewFamilyFromMarriage(subjectFamily, subject, relatedFamily, related, now);
            subjectSideFamily = newFamily;
            relatedSideFamily = newFamily;
        }
        else
        {
            subjectSideFamily = subjectFamily;
            relatedSideFamily = relatedFamily;
        }

        var forward = subjectSideFamily.AddKinship(subject.Id, related.Id, related.Name, related.Cpf, command.Type, now);
        if (forward.IsError)
        {
            return forward.Errors;
        }

        var backward = relatedSideFamily.AddKinship(related.Id, subject.Id, subject.Name, subject.Cpf, command.Type.Inverse(), now);
        if (backward.IsError)
        {
            return backward.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return subject.ToResponse();
    }

    /// <summary>
    /// Cônjuges de famílias diferentes formam uma terceira família: os dois saem de onde
    /// estavam (cada família de origem segue existindo para quem ficou, e é removida se
    /// ficar vazia) e a nova família herda a cota do beneficiário mais antigo dos dois.
    /// </summary>
    private Family FormNewFamilyFromMarriage(
        Family subjectFamily, Beneficiary subject, Family relatedFamily, Beneficiary related, DateTime now)
    {
        var olderFamily = subject.CreatedAtUtc <= related.CreatedAtUtc ? subjectFamily : relatedFamily;
        var newFamily = Family.Create($"{subject.Name} & {related.Name}", olderFamily.Cycle, now).Value;

        subjectFamily.ReleaseBeneficiary(subject.Id);
        relatedFamily.ReleaseBeneficiary(related.Id);
        newFamily.AbsorbBeneficiary(subject);
        newFamily.AbsorbBeneficiary(related);

        familyRepository.Add(newFamily);

        if (subjectFamily.Beneficiaries.Count == 0)
        {
            familyRepository.Remove(subjectFamily);
        }

        if (relatedFamily.Beneficiaries.Count == 0)
        {
            familyRepository.Remove(relatedFamily);
        }

        return newFamily;
    }
}
