using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands.StockItems;

/// <summary>Atualiza nome, unidade, categoria e metas de um item de estoque existente (não altera a quantidade).</summary>
public sealed record UpdateStockItemCommand(
    Guid PointId,
    Guid StockItemId,
    string Name,
    string Unit,
    ItemCategory Category,
    int Goal,
    int AlertThreshold,
    bool IsTracked) : IRequest<ErrorOr<CollectionPointResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class UpdateStockItemValidator : AbstractValidator<UpdateStockItemCommand>
{
    public UpdateStockItemValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.StockItemId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Goal).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AlertThreshold).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateStockItemHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStockItemCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        UpdateStockItemCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var result = point.UpdateStockItem(
            StockItemId.From(command.StockItemId),
            command.Name,
            command.Unit,
            command.Category,
            command.Goal,
            command.AlertThreshold,
            command.IsTracked);
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return point.ToResponse(includeManagementDetails: true);
    }
}
