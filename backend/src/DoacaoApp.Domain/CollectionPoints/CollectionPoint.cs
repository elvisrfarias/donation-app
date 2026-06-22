using DoacaoApp.Domain.CollectionPoints.Events;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;

namespace DoacaoApp.Domain.CollectionPoints;

/// <summary>
/// Ponto de Coleta — Aggregate Root. Pode ser associação, igreja ou entidade pública.
/// É a fronteira para registrar retiradas; expõe endereço/contato para a parte pública.
/// </summary>
public sealed class CollectionPoint : AggregateRoot<CollectionPointId>
{
    private readonly List<Withdrawal> _withdrawals = [];

    // Usado pela materialização do EF Core.
    private CollectionPoint()
    {
        Name = string.Empty;
        Address = null!;
        Contact = null!;
    }

    private CollectionPoint(
        CollectionPointId id,
        string name,
        CollectionPointType type,
        Address address,
        ContactInfo contact,
        DateTime createdAtUtc) : base(id)
    {
        Name = name;
        Type = type;
        Address = address;
        Contact = contact;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public string Name { get; private set; }
    public CollectionPointType Type { get; private set; }
    public Address Address { get; private set; }
    public ContactInfo Contact { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<Withdrawal> Withdrawals => _withdrawals.AsReadOnly();

    public static ErrorOr<CollectionPoint> Create(
        string? name,
        CollectionPointType type,
        Address address,
        ContactInfo contact,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.CollectionPoint.NameRequired;
        }

        var point = new CollectionPoint(CollectionPointId.New(), name.Trim(), type, address, contact, createdAtUtc);
        point.RaiseDomainEvent(new CollectionPointCreated(point.Id, point.Name, point.Type));

        return point;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void UpdateContact(ContactInfo contact) => Contact = contact;

    public void UpdateAddress(Address address) => Address = address;

    /// <summary>
    /// Registra uma retirada aplicando a regra de equidade. O número de retiradas já
    /// feitas pela família NO CICLO VIGENTE (considerando TODOS os pontos do município)
    /// é calculado fora do agregado e injetado aqui — o domínio só decide com base nele.
    /// </summary>
    /// <param name="family">Família que está retirando.</param>
    /// <param name="beneficiary">Beneficiário que comparece (deve pertencer à família).</param>
    /// <param name="familyWithdrawalsInCurrentCycle">
    /// Total de retiradas da família dentro do ciclo vigente, somando todos os pontos.
    /// </param>
    /// <param name="occurredAtUtc">Instante da retirada (UTC).</param>
    /// <param name="notes">Observações opcionais (ex.: itens retirados).</param>
    public ErrorOr<Withdrawal> RegisterWithdrawal(
        Family family,
        Beneficiary beneficiary,
        int familyWithdrawalsInCurrentCycle,
        DateTime occurredAtUtc,
        string? notes)
    {
        if (!IsActive)
        {
            return DomainErrors.CollectionPoint.Inactive;
        }

        if (beneficiary.FamilyId != family.Id)
        {
            return DomainErrors.Withdrawal.BeneficiaryNotInFamily;
        }

        if (familyWithdrawalsInCurrentCycle >= family.Cycle.MaxWithdrawalsPerCycle)
        {
            return DomainErrors.Withdrawal.CycleLimitReached(family.Cycle.MaxWithdrawalsPerCycle);
        }

        var withdrawal = Withdrawal.Create(Id, family.Id, beneficiary.Id, occurredAtUtc, notes);
        _withdrawals.Add(withdrawal);

        RaiseDomainEvent(new WithdrawalRegistered(
            withdrawal.Id, family.Id, Id, beneficiary.Id, occurredAtUtc));

        return withdrawal;
    }
}