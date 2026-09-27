using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands.StockItems;

/// <summary>Uma linha da aba "Metas e avisos": meta, aviso de estoque baixo e se o item é acompanhado.</summary>
public sealed record StockItemSettingEntry(Guid StockItemId, int Goal, int AlertThreshold, bool IsTracked);

/// <summary>
/// Salva em bloco as metas, os avisos de estoque baixo e o acompanhamento de vários itens do ponto
/// (aba "Metas e avisos"). A regra de retirada (ciclo) é configurada à parte, via
/// <see cref="UpdateWithdrawalRuleCommand"/>. Requer Admin ou PointManager.
/// </summary>
public sealed record UpdateStockItemSettingsCommand(
    Guid PointId,
    IReadOnlyList<StockItemSettingEntry> Items) : IRequest<ErrorOr<CollectionPointResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class UpdateStockItemSettingsValidator : AbstractValidator<UpdateStockItemSettingsCommand>
{
    public UpdateStockItemSettingsValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.StockItemId).NotEmpty();
            item.RuleFor(i => i.Goal).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.AlertThreshold).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateStockItemSettingsHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStockItemSettingsCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        UpdateStockItemSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var settings = command.Items.ToDictionary(
            item => StockItemId.From(item.StockItemId),
            item => (item.Goal, item.AlertThreshold, item.IsTracked));

        var result = point.ReplaceStockItemSettings(settings);
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return point.ToResponse(includeManagementDetails: true);
    }
}
