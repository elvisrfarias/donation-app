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
    private List<string> _needs = [];
    private readonly List<StockItem> _stockItems = [];
    private readonly List<BasketTemplate> _basketTemplates = [];
    private readonly List<Category> _categories = [];

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
    /// Imagem (foto) do ponto de coleta no storage. Obrigatória para pontos cadastrados pela
    /// aplicação a partir de agora; nula nos pontos antigos e nos de seed (a UI cai no ícone).
    /// </summary>
    public StoredFileId? ImageFileId { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<Withdrawal> Withdrawals => _withdrawals.AsReadOnly();

    /// <summary>Catálogo de itens de estoque granular deste ponto (ex.: "Arroz 1kg", "Açúcar").</summary>
    public IReadOnlyList<StockItem> StockItems => _stockItems.AsReadOnly();

    /// <summary>Receitas de cesta básica configuradas neste ponto.</summary>
    public IReadOnlyList<BasketTemplate> BasketTemplates => _basketTemplates.AsReadOnly();

    /// <summary>
    /// Catálogo de categorias deste ponto — o que o ponto declara aceitar/disponibilizar (mostrado no
    /// mapa público), o agrupamento do estoque e o checklist livre de uma retirada, tudo pelo mesmo id.
    /// </summary>
    public IReadOnlyList<Category> Categories => _categories.AsReadOnly();

    public static ErrorOr<CollectionPoint> Create(
        string? name,
        CollectionPointType type,
        Address address,
        ContactInfo contact,
        DateTime createdAtUtc,
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
            imageFileId, description, openingHours, needs, createdAtUtc);
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

    /// <summary>Atualiza o texto de apresentação da iniciativa/projeto (nulo quando vazio).</summary>
    public void UpdateDescription(string? description) => Description = NormalizeDescription(description);

    /// <summary>Atualiza o horário de funcionamento (texto livre, nulo quando vazio).</summary>
    public void UpdateOpeningHours(string? openingHours) => OpeningHours = NormalizeOpeningHours(openingHours);

    /// <summary>Redefine a lista de necessidades atuais do ponto (substitui a lista).</summary>
    public void UpdateNeeds(IEnumerable<string>? needs) => _needs = NormalizeNeeds(needs);

    /// <summary>Cadastra um novo item de estoque granular para este ponto (nome único, sem diferenciar maiúsculas/minúsculas).</summary>
    public ErrorOr<StockItemId> AddStockItem(
        string? name, string? unit, CategoryId categoryId, int initialQuantity, int goal, int alertThreshold, bool isTracked)
    {
        var nameValidation = ValidateStockItemName(name, excludingItemId: null);
        if (nameValidation.IsError)
        {
            return nameValidation.FirstError;
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            return DomainErrors.CollectionPoint.StockItemUnitRequired;
        }

        if (initialQuantity < 0)
        {
            return DomainErrors.CollectionPoint.NegativeQuantity;
        }

        var goalValidation = ValidateStockItemGoalAndAlert(goal, alertThreshold);
        if (goalValidation.IsError)
        {
            return goalValidation.FirstError;
        }

        if (_categories.All(c => c.Id != categoryId))
        {
            return DomainErrors.CollectionPoint.CategoryNotFound;
        }

        var item = StockItem.Create(Id, name!.Trim(), unit!.Trim(), categoryId, initialQuantity, goal, alertThreshold, isTracked);
        _stockItems.Add(item);

        return item.Id;
    }

    /// <summary>Atualiza metadados de um item de estoque existente. Não altera a quantidade (ver <see cref="ReplaceStockQuantities"/>).</summary>
    public ErrorOr<Success> UpdateStockItem(
        StockItemId itemId, string? name, string? unit, CategoryId categoryId, int goal, int alertThreshold, bool isTracked)
    {
        var item = _stockItems.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DomainErrors.CollectionPoint.StockItemNotFound;
        }

        var nameValidation = ValidateStockItemName(name, excludingItemId: itemId);
        if (nameValidation.IsError)
        {
            return nameValidation.FirstError;
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            return DomainErrors.CollectionPoint.StockItemUnitRequired;
        }

        var goalValidation = ValidateStockItemGoalAndAlert(goal, alertThreshold);
        if (goalValidation.IsError)
        {
            return goalValidation.FirstError;
        }

        if (_categories.All(c => c.Id != categoryId))
        {
            return DomainErrors.CollectionPoint.CategoryNotFound;
        }

        item.UpdateDetails(name!.Trim(), unit!.Trim(), categoryId, goal, alertThreshold, isTracked);
        return Result.Success;
    }

    /// <summary>Remove um item de estoque. Falha se ele estiver em uso em alguma receita de cesta ou já tiver sido dado em alguma retirada.</summary>
    public ErrorOr<Success> RemoveStockItem(StockItemId itemId)
    {
        var item = _stockItems.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DomainErrors.CollectionPoint.StockItemNotFound;
        }

        var isReferencedByBasketTemplate = _basketTemplates
            .Any(template => template.Items.Any(templateItem => templateItem.StockItemId == itemId));
        if (isReferencedByBasketTemplate)
        {
            return DomainErrors.CollectionPoint.StockItemInUseByBasketTemplate;
        }

        var isReferencedByWithdrawal = _withdrawals
            .Any(withdrawal => withdrawal.Items.Any(withdrawalItem => withdrawalItem.StockItemId == itemId));
        if (isReferencedByWithdrawal)
        {
            return DomainErrors.CollectionPoint.StockItemInUseByWithdrawal;
        }

        _stockItems.Remove(item);
        return Result.Success;
    }

    /// <summary>Substitui em bloco as quantidades de vários itens (aba "Estoque"). Aplica tudo ou nada.</summary>
    public ErrorOr<Success> ReplaceStockQuantities(IReadOnlyDictionary<StockItemId, int> quantities)
    {
        foreach (var (itemId, quantity) in quantities)
        {
            if (_stockItems.All(item => item.Id != itemId))
            {
                return DomainErrors.CollectionPoint.StockItemNotFound;
            }

            if (quantity < 0)
            {
                return DomainErrors.CollectionPoint.NegativeQuantity;
            }
        }

        foreach (var (itemId, quantity) in quantities)
        {
            _stockItems.Single(item => item.Id == itemId).ReplaceQuantity(quantity);
        }

        return Result.Success;
    }

    /// <summary>Soma quantidades recebidas (ex.: doações) ao estoque de vários itens. Aplica tudo ou nada.</summary>
    public ErrorOr<Success> ReceiveStock(IReadOnlyDictionary<StockItemId, int> receivedQuantities)
    {
        foreach (var (itemId, quantity) in receivedQuantities)
        {
            if (_stockItems.All(item => item.Id != itemId))
            {
                return DomainErrors.CollectionPoint.StockItemNotFound;
            }

            if (quantity <= 0)
            {
                return DomainErrors.CollectionPoint.NonPositiveQuantity;
            }
        }

        foreach (var (itemId, quantity) in receivedQuantities)
        {
            _stockItems.Single(item => item.Id == itemId).Increase(quantity);
        }

        return Result.Success;
    }

    /// <summary>Substitui em bloco meta/aviso/monitoramento de vários itens (aba "Metas e avisos"). Aplica tudo ou nada.</summary>
    public ErrorOr<Success> ReplaceStockItemSettings(
        IReadOnlyDictionary<StockItemId, (int Goal, int AlertThreshold, bool IsTracked)> settings)
    {
        foreach (var (itemId, setting) in settings)
        {
            if (_stockItems.All(item => item.Id != itemId))
            {
                return DomainErrors.CollectionPoint.StockItemNotFound;
            }

            var goalValidation = ValidateStockItemGoalAndAlert(setting.Goal, setting.AlertThreshold);
            if (goalValidation.IsError)
            {
                return goalValidation.FirstError;
            }
        }

        foreach (var (itemId, setting) in settings)
        {
            var item = _stockItems.Single(i => i.Id == itemId);
            item.UpdateDetails(item.Name, item.Unit, item.CategoryId, setting.Goal, setting.AlertThreshold, setting.IsTracked);
        }

        return Result.Success;
    }

    /// <summary>Cria uma nova receita de cesta básica, ativa por padrão.</summary>
    public ErrorOr<BasketTemplateId> CreateBasketTemplate(
        string? name, IReadOnlyList<(StockItemId StockItemId, int Quantity)> items)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.CollectionPoint.BasketTemplateNameRequired;
        }

        var itemsValidation = ValidateBasketTemplateItems(items);
        if (itemsValidation.IsError)
        {
            return itemsValidation.FirstError;
        }

        var template = BasketTemplate.Create(Id, name.Trim(), ToTemplateItems(items));
        _basketTemplates.Add(template);

        return template.Id;
    }

    /// <summary>Atualiza nome, itens e status (ativa/inativa) de uma receita de cesta existente.</summary>
    public ErrorOr<Success> UpdateBasketTemplate(
        BasketTemplateId templateId, string? name, IReadOnlyList<(StockItemId StockItemId, int Quantity)> items, bool isActive)
    {
        var template = _basketTemplates.SingleOrDefault(t => t.Id == templateId);
        if (template is null)
        {
            return DomainErrors.CollectionPoint.BasketTemplateNotFound;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.CollectionPoint.BasketTemplateNameRequired;
        }

        var itemsValidation = ValidateBasketTemplateItems(items);
        if (itemsValidation.IsError)
        {
            return itemsValidation.FirstError;
        }

        template.UpdateDetails(name.Trim(), ToTemplateItems(items), isActive);
        return Result.Success;
    }

    /// <summary>
    /// Remove uma receita de cesta. Falha se ela já foi usada em alguma retirada deste ponto — o
    /// histórico de retiradas é uma entidade filha do mesmo agregado, então essa checagem não
    /// precisa de I/O extra (a integridade referencial na Infrastructure, via
    /// <c>DeleteBehavior.Restrict</c>, é só um cinto de segurança adicional).
    /// </summary>
    public ErrorOr<Success> RemoveBasketTemplate(BasketTemplateId templateId)
    {
        var template = _basketTemplates.SingleOrDefault(t => t.Id == templateId);
        if (template is null)
        {
            return DomainErrors.CollectionPoint.BasketTemplateNotFound;
        }

        if (_withdrawals.Any(w => w.BasketTemplateId == templateId))
        {
            return DomainErrors.CollectionPoint.BasketTemplateInUseByWithdrawal;
        }

        _basketTemplates.Remove(template);
        return Result.Success;
    }

    /// <summary>Cadastra uma nova categoria para este ponto (nome único, sem diferenciar maiúsculas/minúsculas).</summary>
    public ErrorOr<CategoryId> AddCategory(string? name, string? icon)
    {
        var nameValidation = ValidateCategoryName(name, excludingCategoryId: null);
        if (nameValidation.IsError)
        {
            return nameValidation.FirstError;
        }

        if (string.IsNullOrWhiteSpace(icon))
        {
            return DomainErrors.CollectionPoint.CategoryIconRequired;
        }

        var category = Category.Create(Id, name!.Trim(), icon!.Trim());
        _categories.Add(category);

        return category.Id;
    }

    /// <summary>Atualiza nome e ícone de uma categoria existente.</summary>
    public ErrorOr<Success> UpdateCategory(CategoryId id, string? name, string? icon)
    {
        var category = _categories.SingleOrDefault(c => c.Id == id);
        if (category is null)
        {
            return DomainErrors.CollectionPoint.CategoryNotFound;
        }

        var nameValidation = ValidateCategoryName(name, excludingCategoryId: id);
        if (nameValidation.IsError)
        {
            return nameValidation.FirstError;
        }

        if (string.IsNullOrWhiteSpace(icon))
        {
            return DomainErrors.CollectionPoint.CategoryIconRequired;
        }

        category.UpdateDetails(name!.Trim(), icon!.Trim());
        return Result.Success;
    }

    /// <summary>
    /// Remove uma categoria. Falha se ela estiver em uso em algum item de estoque — como uma retirada só
    /// referencia itens de estoque (nunca a categoria diretamente), e um item de estoque usado em alguma
    /// retirada não pode ser removido (<see cref="RemoveStockItem"/>), essa única checagem já protege
    /// transitivamente uma categoria com histórico de retiradas.
    /// </summary>
    public ErrorOr<Success> RemoveCategory(CategoryId id)
    {
        var category = _categories.SingleOrDefault(c => c.Id == id);
        if (category is null)
        {
            return DomainErrors.CollectionPoint.CategoryNotFound;
        }

        if (_stockItems.Any(item => item.CategoryId == id))
        {
            return DomainErrors.CollectionPoint.CategoryInUseByStockItem;
        }

        _categories.Remove(category);
        return Result.Success;
    }

    /// <summary>
    /// Calcula quantas cestas completas dá para montar agora com o estoque atual, e o que falta para
    /// montar mais uma. Cálculo puro, sem I/O. Retorna zero cestas e nenhum item se a receita não existir.
    /// </summary>
    public BasketFeasibility CalculateBasketFeasibility(BasketTemplateId templateId)
    {
        var template = _basketTemplates.SingleOrDefault(t => t.Id == templateId);
        if (template is null || template.Items.Count == 0)
        {
            return new BasketFeasibility(0, []);
        }

        var completeBaskets = template.Items.Min(PossibleBasketsFor);
        var missing = template.Items.Select(item => BuildGap(item, completeBaskets)).ToList();

        return new BasketFeasibility(completeBaskets, missing);
    }

    private int PossibleBasketsFor(BasketTemplateItem item)
    {
        if (item.Quantity == 0)
        {
            return int.MaxValue;
        }

        var stockItem = _stockItems.Single(i => i.Id == item.StockItemId);
        return stockItem.Quantity / item.Quantity;
    }

    private BasketItemGap BuildGap(BasketTemplateItem item, int completeBaskets)
    {
        var stockItem = _stockItems.Single(i => i.Id == item.StockItemId);
        var neededForNextBasket = Math.Max(0, (item.Quantity * (completeBaskets + 1)) - stockItem.Quantity);

        return new BasketItemGap(item.StockItemId, stockItem.Name, item.Quantity, stockItem.Quantity, neededForNextBasket);
    }

    private static IEnumerable<BasketTemplateItem> ToTemplateItems(
        IReadOnlyList<(StockItemId StockItemId, int Quantity)> items) =>
        items.Select(i => new BasketTemplateItem(i.StockItemId, i.Quantity));

    private ErrorOr<Success> ValidateBasketTemplateItems(IReadOnlyList<(StockItemId StockItemId, int Quantity)> items)
    {
        if (items.Count == 0)
        {
            return DomainErrors.CollectionPoint.BasketTemplateItemsRequired;
        }

        if (items.Any(item => item.Quantity <= 0))
        {
            return DomainErrors.CollectionPoint.BasketTemplateInvalidQuantity;
        }

        if (items.Select(item => item.StockItemId).Distinct().Count() != items.Count)
        {
            return DomainErrors.CollectionPoint.BasketTemplateDuplicateItem;
        }

        if (items.Any(item => _stockItems.All(stockItem => stockItem.Id != item.StockItemId)))
        {
            return DomainErrors.CollectionPoint.StockItemNotFound;
        }

        return Result.Success;
    }

    private ErrorOr<Success> ValidateStockItemName(string? name, StockItemId? excludingItemId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.CollectionPoint.StockItemNameRequired;
        }

        var trimmedName = name.Trim();
        var isDuplicate = _stockItems.Any(item =>
            item.Id != excludingItemId && string.Equals(item.Name, trimmedName, StringComparison.OrdinalIgnoreCase));

        return isDuplicate ? DomainErrors.CollectionPoint.DuplicateStockItemName : Result.Success;
    }

    private ErrorOr<Success> ValidateCategoryName(string? name, CategoryId? excludingCategoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.CollectionPoint.CategoryNameRequired;
        }

        var trimmedName = name.Trim();
        var isDuplicate = _categories.Any(category =>
            category.Id != excludingCategoryId && string.Equals(category.Name, trimmedName, StringComparison.OrdinalIgnoreCase));

        return isDuplicate ? DomainErrors.CollectionPoint.DuplicateCategoryName : Result.Success;
    }

    private static ErrorOr<Success> ValidateStockItemGoalAndAlert(int goal, int alertThreshold)
    {
        if (goal < 0 || alertThreshold < 0)
        {
            return DomainErrors.StockSetting.NegativeValue;
        }

        if (goal > 0 && alertThreshold > goal)
        {
            return DomainErrors.StockSetting.AlertAboveGoal;
        }

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

    /// <summary>Define (ou troca) a imagem do ponto de coleta.</summary>
    public void SetImage(StoredFileId imageFileId) => ImageFileId = imageFileId;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void UpdateWithdrawalRule(WithdrawalCycle rule) => WithdrawalRule = rule;

    public void UpdateContact(ContactInfo contact) => Contact = contact;

    public void UpdateAddress(Address address) => Address = address;

    /// <summary>
    /// Registra uma retirada aplicando a regra de equidade. O número de retiradas com cesta básica já
    /// feitas pelo beneficiário NO CICLO VIGENTE deste ponto (considerando o histórico de TODOS os
    /// pontos do município) é calculado fora do agregado e injetado aqui — o domínio só decide com
    /// base nele e na <see cref="WithdrawalRule"/> deste ponto.
    /// </summary>
    /// <param name="beneficiary">Beneficiário que está retirando.</param>
    /// <param name="items">Itens livres retirados na visita (roupas, calçados…), com quantidade — não sujeitos ao limite do ciclo, mas dão baixa em estoque.</param>
    /// <param name="beneficiaryWithdrawalsInCurrentCycle">
    /// Total de retiradas com cesta básica pelo beneficiário dentro do ciclo vigente, somando todos os pontos.
    /// </param>
    /// <param name="occurredAtUtc">Instante da retirada (UTC).</param>
    /// <param name="notes">Observações opcionais (ex.: itens retirados).</param>
    /// <param name="basket">Receita de cesta básica retirada nesta visita, se houver, e a quantidade de cestas.</param>
    public ErrorOr<Withdrawal> RegisterWithdrawal(
        Beneficiary beneficiary,
        IReadOnlyCollection<WithdrawalItem> items,
        int beneficiaryWithdrawalsInCurrentCycle,
        DateTime occurredAtUtc,
        string? notes,
        (BasketTemplateId Id, int Quantity)? basket = null)
    {
        if (!IsActive)
        {
            return DomainErrors.CollectionPoint.Inactive;
        }

        var itemsValidation = ValidateItemsForWithdrawal(items);
        if (itemsValidation.IsError)
        {
            return itemsValidation.FirstError;
        }

        var basketValidation = ValidateBasketForWithdrawal(basket);
        if (basketValidation.IsError)
        {
            return basketValidation.FirstError;
        }

        if (basket is not null && beneficiaryWithdrawalsInCurrentCycle >= WithdrawalRule.MaxWithdrawalsPerCycle)
        {
            return DomainErrors.Withdrawal.CycleLimitReached(WithdrawalRule.MaxWithdrawalsPerCycle);
        }

        ApplyItemConsumption(items);
        if (basket is not null)
        {
            ApplyBasketConsumption(basket.Value);
        }

        var withdrawal = Withdrawal.Create(Id, beneficiary.Id, items, occurredAtUtc, notes, basket);
        _withdrawals.Add(withdrawal);

        RaiseDomainEvent(new WithdrawalRegistered(
            withdrawal.Id, Id, beneficiary.Id, withdrawal.Items, occurredAtUtc));

        return withdrawal;
    }

    /// <summary>
    /// Corrige os itens/observações/cesta de uma retirada já registrada. Só a retirada mais recente do
    /// beneficiário pode ser editada — evita que um ajuste tardio destoe do estoque e do histórico
    /// de ciclo já consolidados desde então. Quem determina isso (o histórico é municipal, fora
    /// deste agregado) é o Handler, que injeta o resultado em <paramref name="isMostRecentForBeneficiary"/>,
    /// assim como injeta <paramref name="beneficiaryWithdrawalsInCurrentCycleExcludingThis"/>. Trocar de
    /// cesta (ou de quantidade) numa edição devolve o estoque reservado pela cesta antiga antes de validar
    /// e aplicar a nova, para não bloquear a troca por falta de estoque que na prática já está livre.
    /// </summary>
    public ErrorOr<Success> UpdateWithdrawal(
        WithdrawalId withdrawalId,
        bool isMostRecentForBeneficiary,
        IReadOnlyCollection<WithdrawalItem> items,
        string? notes,
        int beneficiaryWithdrawalsInCurrentCycleExcludingThis,
        (BasketTemplateId Id, int Quantity)? basket = null)
    {
        var withdrawal = _withdrawals.SingleOrDefault(w => w.Id == withdrawalId);
        if (withdrawal is null)
        {
            return DomainErrors.Withdrawal.NotFound;
        }

        if (!isMostRecentForBeneficiary)
        {
            return DomainErrors.Withdrawal.OnlyMostRecentIsEditable;
        }

        var itemsValidation = ValidateItemsForWithdrawal(items, withdrawal.Items);
        if (itemsValidation.IsError)
        {
            return itemsValidation.FirstError;
        }

        var basketValidation = ValidateBasketForWithdrawal(basket, withdrawal.BasketTemplateId, withdrawal.BasketQuantity);
        if (basketValidation.IsError)
        {
            return basketValidation.FirstError;
        }

        if (basket is not null && beneficiaryWithdrawalsInCurrentCycleExcludingThis >= WithdrawalRule.MaxWithdrawalsPerCycle)
        {
            return DomainErrors.Withdrawal.CycleLimitReached(WithdrawalRule.MaxWithdrawalsPerCycle);
        }

        RevertItemConsumption(withdrawal.Items);
        ApplyItemConsumption(items);

        RevertBasketConsumption(withdrawal.BasketTemplateId, withdrawal.BasketQuantity);
        if (basket is not null)
        {
            ApplyBasketConsumption(basket.Value);
        }

        withdrawal.UpdateDetails(items, notes, basket);
        return Result.Success;
    }

    private ErrorOr<Success> ValidateBasketForWithdrawal(
        (BasketTemplateId Id, int Quantity)? basket,
        BasketTemplateId? previousTemplateId = null,
        int? previousQuantity = null)
    {
        if (basket is null)
        {
            return Result.Success;
        }

        var template = _basketTemplates.SingleOrDefault(t => t.Id == basket.Value.Id);
        if (template is null)
        {
            return DomainErrors.Withdrawal.BasketTemplateNotFound;
        }

        if (!template.IsActive)
        {
            return DomainErrors.Withdrawal.BasketTemplateInactive;
        }

        var hasEnoughStock = template.Items.All(item =>
            AvailableQuantityFor(item.StockItemId, previousTemplateId, previousQuantity) >= item.Quantity * basket.Value.Quantity);

        return hasEnoughStock ? Result.Success : DomainErrors.Withdrawal.InsufficientStock;
    }

    private int AvailableQuantityFor(StockItemId stockItemId, BasketTemplateId? previousTemplateId, int? previousQuantity)
    {
        var currentQuantity = _stockItems.Single(item => item.Id == stockItemId).Quantity;
        if (previousTemplateId is null || previousQuantity is null)
        {
            return currentQuantity;
        }

        var previousTemplate = _basketTemplates.SingleOrDefault(t => t.Id == previousTemplateId.Value);
        var previousItem = previousTemplate?.Items.SingleOrDefault(i => i.StockItemId == stockItemId);

        return previousItem is null
            ? currentQuantity
            : currentQuantity + (previousItem.Quantity * previousQuantity.Value);
    }

    private void ApplyBasketConsumption((BasketTemplateId Id, int Quantity) basket)
    {
        var template = _basketTemplates.Single(t => t.Id == basket.Id);
        foreach (var item in template.Items)
        {
            _stockItems.Single(stockItem => stockItem.Id == item.StockItemId).Decrease(item.Quantity * basket.Quantity);
        }
    }

    private void RevertBasketConsumption(BasketTemplateId? templateId, int? quantity)
    {
        if (templateId is null || quantity is null)
        {
            return;
        }

        var template = _basketTemplates.SingleOrDefault(t => t.Id == templateId.Value);
        if (template is null)
        {
            return;
        }

        foreach (var item in template.Items)
        {
            var stockItem = _stockItems.SingleOrDefault(s => s.Id == item.StockItemId);
            stockItem?.Increase(item.Quantity * quantity.Value);
        }
    }

    private ErrorOr<Success> ValidateItemsForWithdrawal(
        IReadOnlyCollection<WithdrawalItem> items, IReadOnlyCollection<WithdrawalItem>? previousItems = null)
    {
        if (items.Count == 0)
        {
            return DomainErrors.Withdrawal.ItemsRequired;
        }

        if (items.Any(item => item.Quantity <= 0))
        {
            return DomainErrors.Withdrawal.InvalidItemQuantity;
        }

        if (items.Select(item => item.StockItemId).Distinct().Count() != items.Count)
        {
            return DomainErrors.Withdrawal.DuplicateItem;
        }

        if (items.Any(item => _stockItems.All(stockItem => stockItem.Id != item.StockItemId)))
        {
            return DomainErrors.CollectionPoint.StockItemNotFound;
        }

        var hasEnoughStock = items.All(item =>
            AvailableQuantityForItem(item.StockItemId, previousItems) >= item.Quantity);

        return hasEnoughStock ? Result.Success : DomainErrors.Withdrawal.InsufficientStock;
    }

    private int AvailableQuantityForItem(StockItemId stockItemId, IReadOnlyCollection<WithdrawalItem>? previousItems)
    {
        var currentQuantity = _stockItems.Single(item => item.Id == stockItemId).Quantity;
        var previouslyReservedQuantity = previousItems?.SingleOrDefault(item => item.StockItemId == stockItemId)?.Quantity ?? 0;
        return currentQuantity + previouslyReservedQuantity;
    }

    private void ApplyItemConsumption(IReadOnlyCollection<WithdrawalItem> items)
    {
        foreach (var item in items)
        {
            _stockItems.Single(stockItem => stockItem.Id == item.StockItemId).Decrease(item.Quantity);
        }
    }

    private void RevertItemConsumption(IReadOnlyCollection<WithdrawalItem>? previousItems)
    {
        if (previousItems is null || previousItems.Count == 0)
        {
            return;
        }

        foreach (var item in previousItems)
        {
            var stockItem = _stockItems.SingleOrDefault(s => s.Id == item.StockItemId);
            stockItem?.Increase(item.Quantity);
        }
    }
}
