using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands.BasketTemplates;

/// <summary>
/// Remove uma receita de cesta básica. Falha se ela já foi usada em alguma retirada — nesse caso,
/// oriente o gestor a desativá-la em vez de removê-la.
/// </summary>
public sealed record RemoveBasketTemplateCommand(Guid PointId, Guid TemplateId)
    : IRequest<ErrorOr<Deleted>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class RemoveBasketTemplateValidator : AbstractValidator<RemoveBasketTemplateCommand>
{
    public RemoveBasketTemplateValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
    }
}

public sealed class RemoveBasketTemplateHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveBasketTemplateCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        RemoveBasketTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var result = point.RemoveBasketTemplate(BasketTemplateId.From(command.TemplateId));
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
