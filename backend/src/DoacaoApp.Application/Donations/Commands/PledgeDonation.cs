using DoacaoApp.Application.Common.Abstractions;
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
/// Avisa um ponto de coleta de que alguém pretende doar. Endpoint público: o doador informa o
/// que vai entregar e, opcionalmente, um telefone para o gestor combinar a entrega.
/// </summary>
public sealed record PledgeDonationCommand(
    Guid CollectionPointId,
    string DonorName,
    string? DonorPhone,
    string Description) : IRequest<ErrorOr<DonationResponse>>;

public sealed class PledgeDonationValidator : AbstractValidator<PledgeDonationCommand>
{
    public PledgeDonationValidator()
    {
        RuleFor(x => x.CollectionPointId).NotEmpty();
        RuleFor(x => x.DonorName).NotEmpty().MaximumLength(Donation.MaxDonorNameLength);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(Donation.MaxDescriptionLength);
    }
}

public sealed class PledgeDonationHandler(
    IDonationRepository donationRepository,
    ICollectionPointRepository pointRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<PledgeDonationCommand, ErrorOr<DonationResponse>>
{
    public async Task<ErrorOr<DonationResponse>> Handle(
        PledgeDonationCommand command,
        CancellationToken cancellationToken)
    {
        var point = await pointRepository.GetByIdAsync(
            CollectionPointId.From(command.CollectionPointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        if (!point.IsActive)
        {
            return DomainErrors.CollectionPoint.Inactive;
        }

        var donation = Donation.Pledge(
            point.Id, command.DonorName, command.DonorPhone, command.Description, clock.UtcNow);
        if (donation.IsError)
        {
            return donation.Errors;
        }

        donationRepository.Add(donation.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return donation.Value.ToResponse();
    }
}
