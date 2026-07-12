using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.ValueObjects;

namespace DoacaoApp.Domain.Families;

/// <summary>
/// Beneficiário — Entity DENTRO do agregado <see cref="Family"/>. Não é manipulado
/// diretamente por repositórios: o ciclo de vida passa sempre pela família.
/// </summary>
public sealed class Beneficiary : Entity<BeneficiaryId>
{
    private Beneficiary()
    {
        Name = string.Empty;
        Cpf = null!;
    }

    private Beneficiary(
        BeneficiaryId id,
        FamilyId familyId,
        string name,
        Cpf cpf,
        DateOnly? birthDate,
        CollectionPointId registeredByPointId,
        DateTime createdAtUtc) : base(id)
    {
        FamilyId = familyId;
        Name = name;
        Cpf = cpf;
        BirthDate = birthDate;
        RegisteredByPointId = registeredByPointId;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
    }

    public FamilyId FamilyId { get; private set; }
    public string Name { get; private set; }
    public Cpf Cpf { get; private set; }
    public DateOnly? BirthDate { get; private set; }

    /// <summary>Ponto que cadastrou o beneficiário (cada ponto cadastra os seus).</summary>
    public CollectionPointId RegisteredByPointId { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Criação controlada pelo agregado <see cref="Family"/>.</summary>
    internal static Beneficiary Create(
        FamilyId familyId,
        string name,
        Cpf cpf,
        DateOnly? birthDate,
        CollectionPointId registeredByPointId,
        DateTime createdAtUtc) =>
        new(BeneficiaryId.New(), familyId, name.Trim(), cpf, birthDate, registeredByPointId, createdAtUtc);

    public void Deactivate() => IsActive = false;

    /// <summary>Atualização controlada pelo agregado <see cref="Family"/>.</summary>
    internal void UpdateDetails(
        string name,
        Cpf cpf,
        DateOnly? birthDate,
        CollectionPointId registeredByPointId)
    {
        Name = name.Trim();
        Cpf = cpf;
        BirthDate = birthDate;
        RegisteredByPointId = registeredByPointId;
    }
}