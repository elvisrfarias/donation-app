using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands;

/// <summary>Uma linha do estoque: categoria de item e quantidade disponível.</summary>
public sealed record StockEntry(ItemCategory Category, int Quantity);

/// <summary>Redefine o estoque (itens disponíveis) de um ponto de coleta. Requer Admin ou PointManager.</summary>
public sealed record UpdateCollectionPointStockCommand(
    Guid PointId,
    IReadOnlyList<StockEntry> Items) : IRequest<ErrorOr<CollectionPointResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class UpdateCollectionPointStockValidator : AbstractValidator<UpdateCollectionPointStockCommand>
{
    public UpdateCollectionPointStockValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Category).IsInEnum();
            item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateCollectionPointStockHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCollectionPointStockCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        UpdateCollectionPointStockCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        point.ReplaceStock(command.Items.Select(i => new KeyValuePair<ItemCategory, int>(i.Category, i.Quantity)));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return point.ToResponse(includeStockSettings: true);
    }
}
