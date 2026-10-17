using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Sponsors.Contracts;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Sponsors;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Sponsors.Commands;

/// <summary>
/// Atualiza nome, categoria, site e situação de um patrocinador. A logo é opcional: informe
/// <see cref="LogoFileId"/> só para trocá-la. Desativado, o patrocinador sai da página pública.
/// </summary>
public sealed record UpdateSponsorCommand(
    Guid Id,
    string Name,
    SponsorTier Tier,
    string? WebsiteUrl,
    bool IsActive,
    Guid? LogoFileId) : IRequest<ErrorOr<SponsorResponse>>;

public sealed class UpdateSponsorValidator : AbstractValidator<UpdateSponsorCommand>
{
    public UpdateSponsorValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Sponsor.MaxNameLength);
        RuleFor(x => x.Tier).IsInEnum();
        RuleFor(x => x.WebsiteUrl).MaximumLength(Sponsor.MaxWebsiteUrlLength);
    }
}

public sealed class UpdateSponsorHandler(
    ISponsorRepository repository,
    IStoredFileRepository fileRepository,
    IFileStorage storage,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<UpdateSponsorCommand, ErrorOr<SponsorResponse>>
{
    public async Task<ErrorOr<SponsorResponse>> Handle(UpdateSponsorCommand command, CancellationToken cancellationToken)
    {
        var sponsor = await repository.GetByIdAsync(SponsorId.From(command.Id), cancellationToken);
        if (sponsor is null)
        {
            return DomainErrors.Sponsor.NotFound;
        }

        var updated = sponsor.UpdateDetails(command.Name, command.Tier, command.WebsiteUrl, command.IsActive);
        if (updated.IsError)
        {
            return updated.Errors;
        }

        if (command.LogoFileId is { } logoFileId && logoFileId != sponsor.LogoFileId.Value)
        {
            var newLogo = await SponsorLogo.FindImageAsync(fileRepository, logoFileId, cancellationToken);
            if (newLogo.IsError)
            {
                return newLogo.Errors;
            }

            sponsor.ChangeLogo(newLogo.Value.Id);
            newLogo.Value.AttachToOwner(nameof(Sponsor), sponsor.Id.Value, clock.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return (await new[] { sponsor }.ToResponsesAsync(fileRepository, storage, cancellationToken))[0];
    }
}
