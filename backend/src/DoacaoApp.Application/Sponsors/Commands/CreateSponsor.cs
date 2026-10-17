using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Sponsors.Contracts;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Sponsors;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Sponsors.Commands;

/// <summary>Cadastra um patrocinador, já ativo, com a logo enviada antes por <c>POST /api/files</c>.</summary>
public sealed record CreateSponsorCommand(
    string Name,
    SponsorTier Tier,
    string? WebsiteUrl,
    Guid LogoFileId) : IRequest<ErrorOr<SponsorResponse>>;

public sealed class CreateSponsorValidator : AbstractValidator<CreateSponsorCommand>
{
    public CreateSponsorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Sponsor.MaxNameLength);
        RuleFor(x => x.Tier).IsInEnum();
        RuleFor(x => x.WebsiteUrl).MaximumLength(Sponsor.MaxWebsiteUrlLength);
        RuleFor(x => x.LogoFileId).NotEmpty().WithMessage("Envie a logo do patrocinador.");
    }
}

public sealed class CreateSponsorHandler(
    ISponsorRepository repository,
    IStoredFileRepository fileRepository,
    IFileStorage storage,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<CreateSponsorCommand, ErrorOr<SponsorResponse>>
{
    public async Task<ErrorOr<SponsorResponse>> Handle(CreateSponsorCommand command, CancellationToken cancellationToken)
    {
        var logo = await SponsorLogo.FindImageAsync(fileRepository, command.LogoFileId, cancellationToken);
        if (logo.IsError)
        {
            return logo.Errors;
        }

        var now = clock.UtcNow;
        var sponsor = Sponsor.Create(command.Name, command.Tier, command.WebsiteUrl, logo.Value.Id, now);
        if (sponsor.IsError)
        {
            return sponsor.Errors;
        }

        logo.Value.AttachToOwner(nameof(Sponsor), sponsor.Value.Id.Value, now);
        repository.Add(sponsor.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var logoUrl = await storage.GetPresignedDownloadUrlAsync(
            logo.Value.ObjectKey, logo.Value.OriginalFileName, cancellationToken);

        return sponsor.Value.ToResponse(logoUrl.ToString());
    }
}
