using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands.Categories;

/// <summary>
/// Remove uma categoria do catálogo do ponto. Falha se ela estiver em uso em algum item de estoque
/// ou já tiver sido usada em alguma retirada deste ponto.
/// </summary>
public sealed record RemoveCategoryCommand(Guid PointId, Guid CategoryId)
    : IRequest<ErrorOr<Deleted>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class RemoveCategoryValidator : AbstractValidator<RemoveCategoryCommand>
{
    public RemoveCategoryValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

public sealed class RemoveCategoryHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveCategoryCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        RemoveCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var result = point.RemoveCategory(CategoryId.From(command.CategoryId));
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
