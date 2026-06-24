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

/// <summary>Atualiza um ponto de coleta existente. A imagem é opcional: quando
/// <see cref="ImageFileId"/> é informado, troca a foto; caso contrário, mantém a atual.</summary>
public sealed record UpdateCollectionPointCommand(
    Guid Id,
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
    IReadOnlyList<ItemCategory> AcceptedItems,
    Guid? ImageFileId) : IRequest<ErrorOr<CollectionPointResponse>>;

public sealed class UpdateCollectionPointValidator : AbstractValidator<UpdateCollectionPointCommand>
{
    public UpdateCollectionPointValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.State).NotEmpty().Length(2);
        RuleFor(x => x.ZipCode).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.ResponsibleName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.AcceptedItems)
            .NotEmpty().WithMessage("Informe ao menos uma categoria de itens do ponto de coleta.");
        RuleForEach(x => x.AcceptedItems).IsInEnum();
    }
}

public sealed class UpdateCollectionPointHandler(
    ICollectionPointRepository repository,
    IStoredFileRepository fileRepository,
    IFileStorage storage,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<UpdateCollectionPointCommand, ErrorOr<CollectionPointResponse>>
{
    public async Task<ErrorOr<CollectionPointResponse>> Handle(
        UpdateCollectionPointCommand command,
        CancellationToken cancellationToken)
    {
        var point = await repository.GetByIdAsync(CollectionPointId.From(command.Id), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var address = Address.Create(
            command.Street, command.Number, command.Complement,
            command.District, command.City, command.State, command.ZipCode);

        var contact = ContactInfo.Create(command.ResponsibleName, command.Email, command.Phone);

        List<Error> voErrors = [.. address.ErrorsOrEmptyList, .. contact.ErrorsOrEmptyList];
        if (voErrors.Count > 0)
        {
            return voErrors;
        }

        var detailsResult = point.UpdateDetails(command.Name, command.Type);
        if (detailsResult.IsError)
        {
            return detailsResult.Errors;
        }

        point.UpdateAddress(address.Value);
        point.UpdateContact(contact.Value);
        point.UpdateAcceptedItems(command.AcceptedItems);

        var now = clock.UtcNow;

        if (command.ImageFileId is { } imageFileId && point.ImageFileId?.Value != imageFileId)
        {
            var newImage = await fileRepository.GetByIdAsync(StoredFileId.From(imageFileId), cancellationToken);
            if (newImage is null || newImage.Status == StoredFileStatus.Deleted)
            {
                return DomainErrors.StoredFile.NotFound;
            }

            if (!newImage.IsImage)
            {
                return DomainErrors.StoredFile.UnsupportedContentType(newImage.ContentType);
            }

            point.SetImage(newImage.Id);
            newImage.AttachToOwner(nameof(CollectionPoint), point.Id.Value, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        string? imageUrl = null;
        if (point.ImageFileId is { } currentImageId)
        {
            var image = await fileRepository.GetByIdAsync(currentImageId, cancellationToken);
            if (image is not null && image.Status != StoredFileStatus.Deleted)
            {
                var url = await storage.GetPresignedDownloadUrlAsync(
                    image.ObjectKey, image.OriginalFileName, cancellationToken);
                imageUrl = url.ToString();
            }
        }

        return point.ToResponse(imageUrl);
    }
}
