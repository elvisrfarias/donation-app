using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands.StockItems;

/// <summary>Uma linha da aba "Estoque": item e a nova quantidade disponível.</summary>
public sealed record StockQuantityEntry(Guid StockItemId, int Quantity);

/// <summary>Salva em bloco as quantidades de vários itens do ponto (aba "Estoque"). Requer Admin ou PointManager.</summary>
public sealed record UpdateStockQuantitiesCommand(
    Guid PointId,
    IReadOnlyList<StockQuantityEntry> Items) : IRequest<ErrorOr<CollectionPointResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class UpdateStockQuantitiesValidator : AbstractValidator<UpdateStockQuantitiesCommand>
{
    public UpdateStockQuantitiesValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.StockItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateStockQuantitiesHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStockQuantitiesCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        UpdateStockQuantitiesCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var quantities = command.Items.ToDictionary(
            item => StockItemId.From(item.StockItemId), item => item.Quantity);

        var result = point.ReplaceStockQuantities(quantities);
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return point.ToResponse(includeManagementDetails: true);
    }
}
