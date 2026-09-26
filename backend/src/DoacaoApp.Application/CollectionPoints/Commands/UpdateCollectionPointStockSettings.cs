using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands;

/// <summary>Configuração de estoque de uma categoria: meta, aviso de estoque baixo e acompanhamento.</summary>
public sealed record StockSettingEntry(ItemCategory Category, int Goal, int AlertThreshold, bool IsTracked);

/// <summary>
/// Redefine as metas, os avisos de estoque baixo e as categorias acompanhadas de um ponto de coleta,
/// e a regra de retirada do ponto (ciclo + máximo por pessoa; quando omitidas, mantém a atual).
/// Requer Admin ou PointManager (que só configura o próprio ponto).
/// </summary>
public sealed record UpdateCollectionPointStockSettingsCommand(
    Guid PointId,
    IReadOnlyList<StockSettingEntry> Items,
    CycleType? CycleType = null,
    int? MaxWithdrawalsPerCycle = null) : IRequest<ErrorOr<CollectionPointResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class UpdateCollectionPointStockSettingsValidator
    : AbstractValidator<UpdateCollectionPointStockSettingsCommand>
{
    public UpdateCollectionPointStockSettingsValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.CycleType).IsInEnum().When(x => x.CycleType is not null);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Category).IsInEnum();
            item.RuleFor(i => i.Goal).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.AlertThreshold).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateCollectionPointStockSettingsHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCollectionPointStockSettingsCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        UpdateCollectionPointStockSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var rule = WithdrawalCycle.Create(
            command.CycleType ?? point.WithdrawalRule.Type,
            command.MaxWithdrawalsPerCycle ?? point.WithdrawalRule.MaxWithdrawalsPerCycle);
        if (rule.IsError)
        {
            return rule.Errors;
        }

        var replaced = point.ReplaceStockSettings(command.Items.Select(i =>
            new KeyValuePair<ItemCategory, StockSetting>(
                i.Category, new StockSetting(i.Goal, i.AlertThreshold, i.IsTracked))));

        if (replaced.IsError)
        {
            return replaced.Errors;
        }

        point.UpdateWithdrawalRule(rule.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return point.ToResponse(includeStockSettings: true);
    }
}
