using DoacaoApp.Domain.Beneficiaries.Events;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;

namespace DoacaoApp.Domain.Beneficiaries;

/// <summary>
/// Beneficiário — Aggregate Root e entidade central de rastreabilidade. Guarda os dados da pessoa,
/// o ponto que a cadastrou, os pontos que a atendem e os vínculos de parentesco declarados. O limite de retiradas é regra
/// do ponto de coleta (<see cref="CollectionPoints.CollectionPoint.WithdrawalRule"/>).
/// </summary>
public sealed class Beneficiary : AggregateRoot<BeneficiaryId>
{
    public const int MaxPhones = 2;

    private readonly List<BeneficiaryKinship> _kinships = [];
    private readonly List<BeneficiaryPointLink> _pointLinks = [];
    private List<PhoneNumber> _phones = [];

    private Beneficiary()
    {
        Name = string.Empty;
        Cpf = null!;
    }

    private Beneficiary(
        BeneficiaryId id,
        string name,
        Cpf cpf,
        Email email,
        List<PhoneNumber> phones,
        Address? address,
        DateOnly? birthDate,
        CollectionPointId registeredByPointId,
        DateTime createdAtUtc) : base(id)
    {
        Name = name;
        Cpf = cpf;
        Email = email;
        _phones = phones;
        Address = address;
        BirthDate = birthDate;
        RegisteredByPointId = registeredByPointId;
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
        _pointLinks.Add(new BeneficiaryPointLink(registeredByPointId, createdAtUtc));
    }

    public string Name { get; private set; }
    public Cpf Cpf { get; private set; }
    public DateOnly? BirthDate { get; private set; }

    /// <summary>E-mail de contato. Obrigatório no cadastro e na atualização; nulo só em cadastros anteriores a essa exigência.</summary>
    public Email? Email { get; private set; }

    /// <summary>Telefones de contato: o principal é obrigatório e são no máximo <see cref="MaxPhones"/>.</summary>
    public IReadOnlyList<PhoneNumber> Phones => _phones.AsReadOnly();

    /// <summary>Endereço de residência; opcional (só as coordenadas do mapa não são usadas aqui).</summary>
    public Address? Address { get; private set; }

    /// <summary>Ponto que fez o cadastro original do beneficiário (rastreabilidade).</summary>
    public CollectionPointId RegisteredByPointId { get; private set; }

    /// <summary>Pontos que atendem o beneficiário — só eles o enxergam e editam.</summary>
    public IReadOnlyList<BeneficiaryPointLink> PointLinks => _pointLinks.AsReadOnly();

    public IEnumerable<CollectionPointId> LinkedPointIds => _pointLinks.Select(link => link.PointId);

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Vínculos de parentesco declarados por este beneficiário com outros.</summary>
    public IReadOnlyList<BeneficiaryKinship> Kinships => _kinships.AsReadOnly();

    public static ErrorOr<Beneficiary> Create(
        string? name,
        Cpf cpf,
        Email email,
        IReadOnlyCollection<PhoneNumber> phones,
        Address? address,
        DateOnly? birthDate,
        CollectionPointId registeredByPointId,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.Beneficiary.NameRequired;
        }

        if (birthDate is null)
        {
            return DomainErrors.Beneficiary.BirthDateRequired;
        }

        var distinctPhones = DistinctPhones(phones);
        var phonesError = ValidatePhones(distinctPhones);
        if (phonesError is not null)
        {
            return phonesError.Value;
        }

        var beneficiary = new Beneficiary(
            BeneficiaryId.New(), name.Trim(), cpf, email, distinctPhones, address, birthDate, registeredByPointId, createdAtUtc);

        beneficiary.RaiseDomainEvent(new BeneficiaryRegistered(beneficiary.Id, registeredByPointId));

        return beneficiary;
    }

    public ErrorOr<Success> UpdateDetails(
        string? name,
        Cpf cpf,
        Email email,
        IReadOnlyCollection<PhoneNumber> phones,
        Address? address,
        DateOnly? birthDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.Beneficiary.NameRequired;
        }

        if (birthDate is null)
        {
            return DomainErrors.Beneficiary.BirthDateRequired;
        }

        var distinctPhones = DistinctPhones(phones);
        var phonesError = ValidatePhones(distinctPhones);
        if (phonesError is not null)
        {
            return phonesError.Value;
        }

        Name = name.Trim();
        Cpf = cpf;
        Email = email;
        _phones = distinctPhones;
        Address = address;
        BirthDate = birthDate;

        return Result.Success;
    }

    private static Error? ValidatePhones(IReadOnlyCollection<PhoneNumber> phones) => phones.Count switch
    {
        0 => DomainErrors.Beneficiary.PhoneRequired,
        > MaxPhones => DomainErrors.Beneficiary.TooManyPhones,
        _ => null,
    };

    private static List<PhoneNumber> DistinctPhones(IReadOnlyCollection<PhoneNumber> phones) =>
        phones.Distinct().ToList();

    public bool IsLinkedTo(CollectionPointId pointId) => _pointLinks.Any(link => link.PointId == pointId);

    /// <summary>Vincula o beneficiário a mais um ponto de coleta que passa a atendê-lo.</summary>
    public ErrorOr<Success> LinkToPoint(CollectionPointId pointId, DateTime linkedAtUtc)
    {
        if (IsLinkedTo(pointId))
        {
            return DomainErrors.Beneficiary.AlreadyLinkedToPoint;
        }

        _pointLinks.Add(new BeneficiaryPointLink(pointId, linkedAtUtc));

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
