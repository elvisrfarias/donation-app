using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
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
    string Phone) : IRequest<ErrorOr<CollectionPointResponse>>;

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
    }
}

public sealed class CreateCollectionPointHandler(
    ICollectionPointRepository repository,
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
            command.District, command.City, command.State, command.ZipCode);

        var contact = ContactInfo.Create(command.ResponsibleName, command.Email, command.Phone);

        // Acumula erros dos Value Objects para uma resposta de validação completa.
        List<Error> voErrors = [.. address.ErrorsOrEmptyList, .. contact.ErrorsOrEmptyList];
        if (voErrors.Count > 0)
        {
            return voErrors;
        }

        var pointResult = CollectionPoint.Create(
            command.Name, command.Type, address.Value, contact.Value, clock.UtcNow);

        if (pointResult.IsError)
        {
            return pointResult.Errors;
        }

        repository.Add(pointResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return pointResult.Value.ToResponse();
    }
}