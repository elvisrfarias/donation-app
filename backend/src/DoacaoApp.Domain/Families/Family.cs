using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Families.Events;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;

namespace DoacaoApp.Domain.Families;

/// <summary>
/// Família — Aggregate Root e entidade central de rastreabilidade. Concentra a regra
/// de equidade (limite de retiradas por ciclo) e a coleção de beneficiários.
/// </summary>
public sealed class Family : AggregateRoot<FamilyId>
{
    private readonly List<Beneficiary> _beneficiaries = [];

    // Usado pela materialização do EF Core.
    private Family()
    {
        Name = string.Empty;
        Cycle = null!;
    }

    private Family(FamilyId id, string name, WithdrawalCycle cycle, DateTime createdAtUtc) : base(id)
    {
        Name = name;
        Cycle = cycle;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Identificação da família (ex.: "Família Silva" ou um código de referência).</summary>
    public string Name { get; private set; }

    /// <summary>Regra de equidade: tipo do ciclo + máximo de retiradas por ciclo.</summary>
    public WithdrawalCycle Cycle { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<Beneficiary> Beneficiaries => _beneficiaries.AsReadOnly();

    public static ErrorOr<Family> Create(string? name, WithdrawalCycle cycle, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.Family.NameRequired;
        }

        return new Family(FamilyId.New(), name.Trim(), cycle, createdAtUtc);
    }

    public void UpdateCycle(WithdrawalCycle cycle) => Cycle = cycle;

    /// <summary>
    /// Cadastra e vincula um beneficiário a esta família. Garante CPF único dentro da
    /// família e dispara <see cref="BeneficiaryLinkedToFamily"/>.
    /// </summary>
    public ErrorOr<Beneficiary> AddBeneficiary(
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

        if (_beneficiaries.Any(b => b.Cpf == cpf))
        {
            return DomainErrors.Family.DuplicatedBeneficiaryCpf;
        }

        var beneficiary = Beneficiary.Create(Id, name, cpf, birthDate, registeredByPointId, createdAtUtc);
        _beneficiaries.Add(beneficiary);

        RaiseDomainEvent(new BeneficiaryLinkedToFamily(beneficiary.Id, Id, registeredByPointId));

        return beneficiary;
    }

    public Beneficiary? FindBeneficiary(BeneficiaryId beneficiaryId) =>
        _beneficiaries.SingleOrDefault(b => b.Id == beneficiaryId);
}