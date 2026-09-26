using DoacaoApp.Domain.CollectionPoints.Events;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.Storage;
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
    private readonly List<ItemCategory> _acceptedItems = [];
    private List<string> _needs = [];
    private Dictionary<ItemCategory, int> _stock = [];
    private Dictionary<ItemCategory, StockSetting> _stockSettings = [];

    private CollectionPoint()
    {
        Name = string.Empty;
        Address = null!;
        Contact = null!;
        WithdrawalRule = null!;
    }

    private CollectionPoint(
        CollectionPointId id,
        string name,
        CollectionPointType type,
        Address address,
        ContactInfo contact,
        IEnumerable<ItemCategory> acceptedItems,
        StoredFileId? imageFileId,
        string? description,
        string? openingHours,
        IEnumerable<string>? needs,
        DateTime createdAtUtc) : base(id)
    {
        Name = name;
        Type = type;
        Address = address;
        Contact = contact;
        _acceptedItems = Normalize(acceptedItems);
        ImageFileId = imageFileId;
        Description = NormalizeDescription(description);
        OpeningHours = NormalizeOpeningHours(openingHours);
        _needs = NormalizeNeeds(needs);
        CreatedAtUtc = createdAtUtc;
        IsActive = true;
        WithdrawalRule = WithdrawalCycle.AnnualDefault();
    }

    /// <summary>Tamanho máximo da descrição livre da iniciativa/projeto.</summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>Tamanho máximo do texto livre de horário de funcionamento.</summary>
    public const int MaxOpeningHoursLength = 200;

    /// <summary>Nº máximo de itens na lista de necessidades atuais.</summary>
    public const int MaxNeeds = 20;

    /// <summary>Tamanho máximo de cada necessidade da lista.</summary>
    public const int MaxNeedLength = 80;

    public string Name { get; private set; }
    public CollectionPointType Type { get; private set; }
    public Address Address { get; private set; }
    public ContactInfo Contact { get; private set; }

    /// <summary>
    /// Regra de retirada deste ponto: tipo do ciclo + máximo de retiradas por pessoa em cada ciclo. O
    /// histórico de retiradas é somado entre todos os pontos, mas o limite aplicado é o do ponto em que
    /// a retirada acontece.
    /// </summary>
    public WithdrawalCycle WithdrawalRule { get; private set; }

    /// <summary>Texto livre que apresenta a iniciativa/projeto para a divulgação pública.</summary>
    public string? Description { get; private set; }

    /// <summary>Horário de funcionamento em texto livre (ex.: "Seg a Sex, 8h–17h").</summary>
    public string? OpeningHours { get; private set; }

    /// <summary>Itens que o ponto precisa com urgência agora (direciona as doações).</summary>
    public IReadOnlyList<string> Needs => _needs.AsReadOnly();

    /// <summary>
    /// Quantidade disponível por categoria de item (estoque). Categorias ausentes ou zeradas
    /// significam "sem disponibilidade". Ajuda as famílias a saber o que há antes de irem ao ponto.
    /// </summary>
    public IReadOnlyDictionary<ItemCategory, int> Stock => _stock;

    /// <summary>
    /// Metas, avisos de estoque baixo e categorias acompanhadas, por categoria. Categorias sem
    /// entrada usam o padrão (sem meta, aviso padrão, acompanhada).
    /// </summary>
    public IReadOnlyDictionary<ItemCategory, StockSetting> StockSettings => _stockSettings;

    /// <summary>
    /// Imagem (foto) do ponto de coleta no storage. Obrigatória para pontos cadastrados pela
    /// aplicação a partir de agora; nula nos pontos antigos e nos de seed (a UI cai no ícone).
    /// </summary>
    public StoredFileId? ImageFileId { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<Withdrawal> Withdrawals => _withdrawals.AsReadOnly();

    /// <summary>Categorias de itens que o ponto recebe/disponibiliza (ex.: comida, roupas).</summary>
    public IReadOnlyList<ItemCategory> AcceptedItems => _acceptedItems.AsReadOnly();

    public static ErrorOr<CollectionPoint> Create(
        string? name,
        CollectionPointType type,
        Address address,
        ContactInfo contact,
        DateTime createdAtUtc,
        IEnumerable<ItemCategory>? acceptedItems = null,
        StoredFileId? imageFileId = null,
        string? description = null,
        string? openingHours = null,
        IEnumerable<string>? needs = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.CollectionPoint.NameRequired;
        }

        var point = new CollectionPoint(
            CollectionPointId.New(), name.Trim(), type, address, contact,
            acceptedItems ?? [], imageFileId, description, openingHours, needs, createdAtUtc);
        point.RaiseDomainEvent(new CollectionPointCreated(point.Id, point.Name, point.Type));

        return point;
    }

    /// <summary>Atualiza o nome e o tipo do ponto (valida que o nome não fica vazio).</summary>
    public ErrorOr<Updated> UpdateDetails(string? name, CollectionPointType type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.CollectionPoint.NameRequired;
        }

        Name = name.Trim();
        Type = type;
        return Result.Updated;
    }

    /// <summary>Redefine as categorias de itens aceitas pelo ponto (substitui a lista atual).</summary>
    public void UpdateAcceptedItems(IEnumerable<ItemCategory> acceptedItems)
    {
        _acceptedItems.Clear();
        _acceptedItems.AddRange(Normalize(acceptedItems));
    }

    /// <summary>Atualiza o texto de apresentação da iniciativa/projeto (nulo quando vazio).</summary>
    public void UpdateDescription(string? description) => Description = NormalizeDescription(description);

    /// <summary>Atualiza o horário de funcionamento (texto livre, nulo quando vazio).</summary>
    public void UpdateOpeningHours(string? openingHours) => OpeningHours = NormalizeOpeningHours(openingHours);

    /// <summary>Redefine a lista de necessidades atuais do ponto (substitui a lista).</summary>
    public void UpdateNeeds(IEnumerable<string>? needs) => _needs = NormalizeNeeds(needs);

    /// <summary>
    /// Redefine o estoque do ponto (substitui o mapa atual). Quantidades ≤ 0 são descartadas;
    /// quando há entradas repetidas da mesma categoria, prevalece a última.
    /// </summary>
    public void ReplaceStock(IEnumerable<KeyValuePair<ItemCategory, int>>? stock) =>
        _stock = stock is null
            ? []
            : stock
                .Where(kv => kv.Value > 0)
                .GroupBy(kv => kv.Key)
                .ToDictionary(g => g.Key, g => g.Last().Value);

    /// <summary>
    /// Redefine as configurações de estoque (substitui o mapa atual). Quando há entradas repetidas
    /// da mesma categoria, prevalece a última. Valores negativos, ou um aviso acima da meta (quando
    /// há meta), são recusados e o mapa anterior é mantido.
    /// </summary>
    public ErrorOr<Success> ReplaceStockSettings(IEnumerable<KeyValuePair<ItemCategory, StockSetting>>? settings)
    {
        var next = (settings ?? [])
            .GroupBy(kv => kv.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value);

        if (next.Values.Any(setting => setting.Goal < 0 || setting.AlertThreshold < 0))
        {
            return DomainErrors.StockSetting.NegativeValue;
        }

        if (next.Values.Any(setting => setting.Goal > 0 && setting.AlertThreshold > setting.Goal))
        {
            return DomainErrors.StockSetting.AlertAboveGoal;
        }

        _stockSettings = next;
        return Result.Success;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static string? NormalizeOpeningHours(string? openingHours) =>
        string.IsNullOrWhiteSpace(openingHours) ? null : openingHours.Trim();

    private static List<string> NormalizeNeeds(IEnumerable<string>? needs) =>
        needs is null
            ? []
            : [.. needs
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Select(n => n.Length > MaxNeedLength ? n[..MaxNeedLength] : n)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxNeeds)];

    private static List<ItemCategory> Normalize(IEnumerable<ItemCategory> items) =>
        [.. items.Distinct().OrderBy(static i => (int)i)];

    /// <summary>Define (ou troca) a imagem do ponto de coleta.</summary>
    public void SetImage(StoredFileId imageFileId) => ImageFileId = imageFileId;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void UpdateWithdrawalRule(WithdrawalCycle rule) => WithdrawalRule = rule;

    public void UpdateContact(ContactInfo contact) => Contact = contact;

    public void UpdateAddress(Address address) => Address = address;

    /// <summary>
    /// Registra uma retirada aplicando a regra de equidade. O número de retiradas já
    /// feitas pelo beneficiário NO CICLO VIGENTE deste ponto (considerando o histórico de TODOS os
    /// pontos do município) é calculado fora do agregado e injetado aqui — o domínio só decide com
    /// base nele e na <see cref="WithdrawalRule"/> deste ponto.
    /// </summary>
    /// <param name="beneficiary">Beneficiário que está retirando.</param>
    /// <param name="categories">Itens retirados na visita; só as cestas básicas estão sujeitas ao limite do ciclo.</param>
    /// <param name="beneficiaryWithdrawalsInCurrentCycle">
    /// Total de retiradas com cesta básica pelo beneficiário dentro do ciclo vigente, somando todos os pontos.
    /// </param>
    /// <param name="occurredAtUtc">Instante da retirada (UTC).</param>
    /// <param name="notes">Observações opcionais (ex.: itens retirados).</param>
    public ErrorOr<Withdrawal> RegisterWithdrawal(
        Beneficiary beneficiary,
        IReadOnlyCollection<ItemCategory> categories,
        int beneficiaryWithdrawalsInCurrentCycle,
        DateTime occurredAtUtc,
        string? notes)
    {
        if (!IsActive)
        {
            return DomainErrors.CollectionPoint.Inactive;
        }

        var distinctCategories = categories.Distinct().ToList();
        if (distinctCategories.Count == 0)
        {
            return DomainErrors.Withdrawal.CategoriesRequired;
        }

        if (distinctCategories.Any(category => _stock.GetValueOrDefault(category) <= 0))
        {
            return DomainErrors.Withdrawal.ItemUnavailable;
        }

        if (distinctCategories.Any(category => category.CountsTowardsWithdrawalLimit())
            && beneficiaryWithdrawalsInCurrentCycle >= WithdrawalRule.MaxWithdrawalsPerCycle)
        {
            return DomainErrors.Withdrawal.CycleLimitReached(WithdrawalRule.MaxWithdrawalsPerCycle);
        }

        var withdrawal = Withdrawal.Create(Id, beneficiary.Id, distinctCategories, occurredAtUtc, notes);
        _withdrawals.Add(withdrawal);

        RaiseDomainEvent(new WithdrawalRegistered(
            withdrawal.Id, Id, beneficiary.Id, withdrawal.Categories, occurredAtUtc));

        return withdrawal;
    }
}