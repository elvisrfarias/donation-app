using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands.StockItems;

/// <summary>Cadastra um novo item de estoque granular no catálogo do ponto (ex.: "Arroz 1kg").</summary>
public sealed record CreateStockItemCommand(
    Guid PointId,
    string Name,
    string Unit,
    ItemCategory Category,
    int InitialQuantity,
    int Goal,
    int AlertThreshold,
    bool IsTracked) : IRequest<ErrorOr<CollectionPointResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class CreateStockItemValidator : AbstractValidator<CreateStockItemCommand>
{
    public CreateStockItemValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.InitialQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Goal).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AlertThreshold).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateStockItemHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateStockItemCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        CreateStockItemCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var result = point.AddStockItem(
            command.Name,
            command.Unit,
            command.Category,
            command.InitialQuantity,
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
