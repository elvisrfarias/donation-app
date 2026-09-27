using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands.BasketTemplates;

/// <summary>Atualiza nome, itens e status (ativa/inativa) de uma receita de cesta básica existente.</summary>
public sealed record UpdateBasketTemplateCommand(
    Guid PointId,
    Guid TemplateId,
    string Name,
    IReadOnlyList<BasketTemplateItemEntry> Items,
    bool IsActive) : IRequest<ErrorOr<CollectionPointResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class UpdateBasketTemplateValidator : AbstractValidator<UpdateBasketTemplateCommand>
{
    public UpdateBasketTemplateValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.StockItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public sealed class UpdateBasketTemplateHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateBasketTemplateCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        UpdateBasketTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var result = point.UpdateBasketTemplate(
            BasketTemplateId.From(command.TemplateId),
            command.Name,
            CreateBasketTemplateHandler.ToDomainItems(command.Items),
            command.IsActive);
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return point.ToResponse(includeManagementDetails: true);
    }
}
