using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands.StockItems;

/// <summary>Remove um item de estoque do catálogo do ponto. Falha se ele estiver em uso em alguma receita de cesta.</summary>
public sealed record RemoveStockItemCommand(Guid PointId, Guid StockItemId)
    : IRequest<ErrorOr<Deleted>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class RemoveStockItemValidator : AbstractValidator<RemoveStockItemCommand>
{
    public RemoveStockItemValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.StockItemId).NotEmpty();
    }
}

public sealed class RemoveStockItemHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveStockItemCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        RemoveStockItemCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var result = point.RemoveStockItem(StockItemId.From(command.StockItemId));
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
