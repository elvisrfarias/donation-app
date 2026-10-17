using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DoacaoApp.Application.Beneficiaries.Commands;

/// <summary>
/// Vincula um beneficiário já cadastrado (CPF único no município) a mais um ponto de coleta. É a
/// decisão do gestor do ponto: só o próprio ponto pode ser vinculado por ele; o Admin escolhe o ponto.
/// Como o vínculo dá acesso ao cadastro completo, cada vínculo fica registrado no log de auditoria.
/// </summary>
public sealed record LinkBeneficiaryToPointCommand(Guid BeneficiaryId, Guid PointId)
    : IRequest<ErrorOr<BeneficiaryResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class LinkBeneficiaryToPointValidator : AbstractValidator<LinkBeneficiaryToPointCommand>
{
    public LinkBeneficiaryToPointValidator()
    {
        RuleFor(x => x.BeneficiaryId).NotEmpty();
        RuleFor(x => x.PointId).NotEmpty();
    }
}

public sealed class LinkBeneficiaryToPointHandler(
    IBeneficiaryRepository beneficiaryRepository,
    ICollectionPointRepository pointRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    ICurrentUser currentUser,
    ILogger<LinkBeneficiaryToPointHandler> logger)
    : IRequestHandler<LinkBeneficiaryToPointCommand, ErrorOr<BeneficiaryResponse>>
{
    public async Task<ErrorOr<BeneficiaryResponse>> Handle(
        LinkBeneficiaryToPointCommand command,
        CancellationToken cancellationToken)
    {
        var beneficiary = await beneficiaryRepository.GetByIdAsync(
            BeneficiaryId.From(command.BeneficiaryId), cancellationToken);
        if (beneficiary is null)
        {
            return DomainErrors.Beneficiary.NotFound;
        }

        var point = await pointRepository.GetByIdAsync(CollectionPointId.From(command.PointId), cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var linked = beneficiary.LinkToPoint(point.Id, clock.UtcNow);
        if (linked.IsError)
        {
            return linked.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Auditoria: usuário {UserId} vinculou o beneficiário {BeneficiaryId} ao ponto {PointId}.",
            currentUser.UserId,
            beneficiary.Id.Value,
            point.Id.Value);

        return beneficiary.ToResponse();
    }
}
