using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands.BasketTemplates;

/// <summary>Um item da receita de cesta: item de estoque do ponto e quantidade necessária por cesta.</summary>
public sealed record BasketTemplateItemEntry(Guid StockItemId, int Quantity);

/// <summary>Cria uma nova receita de cesta básica para o ponto (ativa por padrão).</summary>
public sealed record CreateBasketTemplateCommand(
    Guid PointId,
    string Name,
    IReadOnlyList<BasketTemplateItemEntry> Items) : IRequest<ErrorOr<CollectionPointResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class CreateBasketTemplateValidator : AbstractValidator<CreateBasketTemplateCommand>
{
    public CreateBasketTemplateValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.StockItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public sealed class CreateBasketTemplateHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBasketTemplateCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        CreateBasketTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var result = point.CreateBasketTemplate(command.Name, ToDomainItems(command.Items));
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return point.ToResponse(includeManagementDetails: true);
    }

    internal static IReadOnlyList<(StockItemId StockItemId, int Quantity)> ToDomainItems(
        IReadOnlyList<BasketTemplateItemEntry> items) =>
        [.. items.Select(item => (StockItemId.From(item.StockItemId), item.Quantity))];
}
