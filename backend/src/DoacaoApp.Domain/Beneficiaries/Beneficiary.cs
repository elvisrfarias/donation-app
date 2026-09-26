using DoacaoApp.Domain.Beneficiaries.Events;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;

namespace DoacaoApp.Domain.Beneficiaries;

/// <summary>
/// Beneficiário — Aggregate Root e entidade central de rastreabilidade. Guarda os dados da pessoa,
/// o ponto que a cadastrou e os vínculos de parentesco declarados. O limite de retiradas é regra
/// do ponto de coleta (<see cref="CollectionPoints.CollectionPoint.WithdrawalRule"/>).
/// </summary>
public sealed class Beneficiary : AggregateRoot<BeneficiaryId>
{
    private readonly List<BeneficiaryKinship> _kinships = [];

    private Beneficiary()
    {
        Name = string.Empty;
        Cpf = null!;
    }

    private Beneficiary(
        BeneficiaryId id,
        string name,
        Cpf cpf,
        DateOnly? birthDate,
        CollectionPointId registeredByPointId,
        DateTime createdAtUtc) : base(id)
    {
        Name = name;
        Cpf = cpf;
        BirthDate = birthDate;
        RegisteredByPointId = registeredByPointId;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public string Name { get; private set; }
    public Cpf Cpf { get; private set; }
    public DateOnly? BirthDate { get; private set; }

    /// <summary>Ponto que cadastrou o beneficiário (cada ponto cadastra os seus).</summary>
    public CollectionPointId RegisteredByPointId { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Vínculos de parentesco declarados por este beneficiário com outros.</summary>
    public IReadOnlyList<BeneficiaryKinship> Kinships => _kinships.AsReadOnly();

    public static ErrorOr<Beneficiary> Create(
        string? name,
        Cpf cpf,
        DateOnly? birthDate,
        CollectionPointId registeredByPointId,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.Beneficiary.NameRequired;
        }

        var beneficiary = new Beneficiary(
            BeneficiaryId.New(), name.Trim(), cpf, birthDate, registeredByPointId, createdAtUtc);

        beneficiary.RaiseDomainEvent(new BeneficiaryRegistered(beneficiary.Id, registeredByPointId));

        return beneficiary;
    }

    public ErrorOr<Success> UpdateDetails(
        string? name,
        Cpf cpf,
        DateOnly? birthDate,
        CollectionPointId registeredByPointId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.Beneficiary.NameRequired;
        }

        Name = name.Trim();
        Cpf = cpf;
        BirthDate = birthDate;
        RegisteredByPointId = registeredByPointId;

        return Result.Success;
    }

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Registra um vínculo de parentesco deste beneficiário com outro. Nome e CPF do parente
    /// são gravados como cópia (snapshot) do momento do vínculo.
    /// </summary>
    public ErrorOr<Success> AddKinship(
        BeneficiaryId relatedBeneficiaryId,
        string relatedBeneficiaryName,
        Cpf relatedBeneficiaryCpf,
        KinshipType type,
        DateTime createdAtUtc)
    {
        if (relatedBeneficiaryId == Id)
        {
            return DomainErrors.Beneficiary.SelfKinshipNotAllowed;
        }

        if (_kinships.Any(k => k.RelatedBeneficiaryId == relatedBeneficiaryId))
        {
            return DomainErrors.Beneficiary.KinshipAlreadyExists;
        }

        _kinships.Add(new BeneficiaryKinship(
            relatedBeneficiaryId, relatedBeneficiaryName, relatedBeneficiaryCpf, type, createdAtUtc));

        return Result.Success;
    }
}
