using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands.StockItems;

/// <summary>
/// Atualiza a regra de retirada do ponto (tipo do ciclo + máximo de retiradas de cesta básica por
/// beneficiário em cada ciclo). Requer Admin ou PointManager (que só configura o próprio ponto).
/// </summary>
public sealed record UpdateWithdrawalRuleCommand(
    Guid PointId,
    CycleType CycleType,
    int MaxWithdrawalsPerCycle) : IRequest<ErrorOr<CollectionPointResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class UpdateWithdrawalRuleValidator : AbstractValidator<UpdateWithdrawalRuleCommand>
{
    public UpdateWithdrawalRuleValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.CycleType).IsInEnum();
    }
}

public sealed class UpdateWithdrawalRuleHandler(
    ICollectionPointRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateWithdrawalRuleCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        UpdateWithdrawalRuleCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var rule = WithdrawalCycle.Create(command.CycleType, command.MaxWithdrawalsPerCycle);
        if (rule.IsError)
        {
            return rule.Errors;
        }

        point.UpdateWithdrawalRule(rule.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return point.ToResponse(includeManagementDetails: true);
    }
}
