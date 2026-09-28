using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Storage;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands;

/// <summary>Cadastra um novo ponto de coleta (associação, igreja ou entidade pública).</summary>
public sealed record CreateCollectionPointCommand(
    string Name,
    CollectionPointType Type,
    string Street,
    string Number,
    string? Complement,
    string District,
    string City,
    string State,
    string ZipCode,
    string ResponsibleName,
    string Email,
    string Phone,
    Guid ImageFileId,
    double? Latitude = null,
    double? Longitude = null,
    string? Description = null,
    string? OpeningHours = null,
    IReadOnlyList<string>? Needs = null) : IRequest<ErrorOr<CollectionPointResponse>>;

public sealed class CreateCollectionPointValidator : AbstractValidator<CreateCollectionPointCommand>
{
    public CreateCollectionPointValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.State).NotEmpty().Length(2);
        RuleFor(x => x.ZipCode).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.ResponsibleName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(CollectionPoint.MaxDescriptionLength);
        RuleFor(x => x.OpeningHours).MaximumLength(CollectionPoint.MaxOpeningHoursLength);
        RuleForEach(x => x.Needs).MaximumLength(CollectionPoint.MaxNeedLength);
        RuleFor(x => x.ImageFileId).NotEmpty().WithMessage("A imagem do ponto de coleta é obrigatória.");
    }
}

public sealed class CreateCollectionPointHandler(
    ICollectionPointRepository repository,
    IStoredFileRepository fileRepository,
    IFileStorage storage,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<CreateCollectionPointCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        CreateCollectionPointCommand command,
        CancellationToken cancellationToken)
    {
        var address = Address.Create(
            command.Street, command.Number, command.Complement,
            command.District, command.City, command.State, command.ZipCode,
            command.Latitude, command.Longitude);

        var contact = ContactInfo.Create(command.ResponsibleName, command.Email, command.Phone);

        List<Error> voErrors = [.. address.ErrorsOrEmptyList, .. contact.ErrorsOrEmptyList];
        if (voErrors.Count > 0)
        {
            return voErrors;
        }

        var image = await fileRepository.GetByIdAsync(StoredFileId.From(command.ImageFileId), cancellationToken);
        if (image is null || image.Status == StoredFileStatus.Deleted)
        {
            return DomainErrors.StoredFile.NotFound;
        }

        if (!image.IsImage)
        {
            return DomainErrors.StoredFile.UnsupportedContentType(image.ContentType);
        }

        var now = clock.UtcNow;
        var pointResult = CollectionPoint.Create(
            command.Name, command.Type, address.Value, contact.Value, now,
            image.Id, command.Description,
            command.OpeningHours, command.Needs);

        if (pointResult.IsError)
        {
            return pointResult.Errors;
        }

        image.AttachToOwner(nameof(CollectionPoint), pointResult.Value.Id.Value, now);

        repository.Add(pointResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var imageUrl = await storage.GetPresignedDownloadUrlAsync(
            image.ObjectKey, image.OriginalFileName, cancellationToken);

        return pointResult.Value.ToResponse(imageUrl.ToString());
    }
}