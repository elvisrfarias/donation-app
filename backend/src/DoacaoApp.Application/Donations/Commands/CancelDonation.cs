using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Donations.Contracts;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Donations;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Donations.Commands;

/// <summary>Cancela uma doação prometida (o doador desistiu ou não compareceu). Requer Admin ou PointManager do ponto.</summary>
public sealed record CancelDonationCommand(Guid PointId, Guid DonationId)
    : IRequest<ErrorOr<DonationResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class CancelDonationValidator : AbstractValidator<CancelDonationCommand>
{
    public CancelDonationValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.DonationId).NotEmpty();
    }
}

public sealed class CancelDonationHandler(
    IDonationRepository donationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<CancelDonationCommand, ErrorOr<DonationResponse>>
{
    public async Task<ErrorOr<DonationResponse>> Handle(
        CancelDonationCommand command,
        CancellationToken cancellationToken)
    {
        var donation = await donationRepository.GetByIdAsync(DonationId.From(command.DonationId), cancellationToken);
        if (donation is null || donation.PointId.Value != command.PointId)
        {
            return DomainErrors.Donation.NotFound;
        }

        var closing = donation.Cancel(clock.UtcNow);
        if (closing.IsError)
        {
            return closing.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return donation.ToResponse();
    }
}
