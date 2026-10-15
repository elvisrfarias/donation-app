using DoacaoApp.Application.CollectionPoints.Commands.StockItems;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Donations.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Donations;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Donations.Commands;

/// <summary>
/// Confirma o recebimento de uma doação prometida ao ponto. Opcionalmente já soma ao estoque
/// as quantidades recebidas de cada item. Requer Admin ou PointManager do ponto.
/// </summary>
public sealed record ReceiveDonationCommand(
    Guid PointId,
    Guid DonationId,
    IReadOnlyList<StockQuantityEntry>? ReceivedStock) : IRequest<ErrorOr<DonationResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class ReceiveDonationValidator : AbstractValidator<ReceiveDonationCommand>
{
    public ReceiveDonationValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.DonationId).NotEmpty();
        RuleForEach(x => x.ReceivedStock).ChildRules(entry =>
        {
            entry.RuleFor(e => e.StockItemId).NotEmpty();
            entry.RuleFor(e => e.Quantity).GreaterThan(0);
        });
    }
}

public sealed class ReceiveDonationHandler(
    IDonationRepository donationRepository,
    ICollectionPointRepository pointRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<ReceiveDonationCommand, ErrorOr<DonationResponse>>
{
    public async Task<ErrorOr<DonationResponse>> Handle(
        ReceiveDonationCommand command,
        CancellationToken cancellationToken)
    {
        var donation = await donationRepository.GetByIdAsync(DonationId.From(command.DonationId), cancellationToken);
        if (donation is null || donation.PointId.Value != command.PointId)
        {
            return DomainErrors.Donation.NotFound;
        }

        var stockResult = await AddReceivedStockAsync(command, cancellationToken);
        if (stockResult.IsError)
        {
            return stockResult.Errors;
        }

        var closing = donation.MarkReceived(clock.UtcNow);
        if (closing.IsError)
        {
            return closing.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return donation.ToResponse();
    }

    private async Task<ErrorOr<Success>> AddReceivedStockAsync(
        ReceiveDonationCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ReceivedStock is not { Count: > 0 })
        {
            return Result.Success;
        }

        var point = await pointRepository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var quantities = command.ReceivedStock
            .GroupBy(entry => StockItemId.From(entry.StockItemId))
            .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Quantity));

        return point.ReceiveStock(quantities);
    }
}
