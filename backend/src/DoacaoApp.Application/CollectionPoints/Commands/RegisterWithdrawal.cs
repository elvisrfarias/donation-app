using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.CollectionPoints.Commands;

/// <summary>
/// Registra uma retirada (uma visita, com um ou mais itens) em um ponto de coleta. Só a cesta básica
/// segue a regra de equidade (limite do ponto no ciclo vigente, somando o histórico de TODOS os pontos
/// do município, e baixa automática no estoque); os demais itens (categorias livres) não têm limite
/// nem dão baixa, mas ficam registrados no histórico do beneficiário.
/// </summary>
public sealed record RegisterWithdrawalCommand(
    Guid PointId,
    Guid BeneficiaryId,
    IReadOnlyList<ItemCategory> Categories,
    DateTime? OccurredAtUtc,
    string? Notes,
    Guid? BasketTemplateId = null,
    int? BasketQuantity = null) : IRequest<ErrorOr<WithdrawalResponse>>, IScopedToCollectionPoint
{
    Guid IScopedToCollectionPoint.CollectionPointId => PointId;
}

public sealed class RegisterWithdrawalValidator : AbstractValidator<RegisterWithdrawalCommand>
{
    public RegisterWithdrawalValidator()
    {
        RuleFor(x => x.PointId).NotEmpty();
        RuleFor(x => x.BeneficiaryId).NotEmpty();
        RuleForEach(x => x.Categories).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.BasketQuantity)
            .GreaterThan(0)
            .When(x => x.BasketTemplateId is not null)
            .WithMessage("Informe a quantidade de cestas retiradas.");
    }
}

public sealed class RegisterWithdrawalHandler(
    ICollectionPointRepository pointRepository,
    IBeneficiaryRepository beneficiaryRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<RegisterWithdrawalCommand, ErrorOr<WithdrawalResponse>>
{
    public async Task<ErrorOr<WithdrawalResponse>> Handle(
        RegisterWithdrawalCommand command,
        CancellationToken cancellationToken)
    {
        var occurredAt = (command.OccurredAtUtc ?? clock.UtcNow).ToUniversalTime();

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

        var (startUtc, endUtc) = point.WithdrawalRule.GetCurrentPeriod(occurredAt);

        var alreadyInCycle = command.BasketTemplateId is not null
            ? await pointRepository.CountBeneficiaryBasketWithdrawalsAsync(
                beneficiary.Id, startUtc, endUtc, cancellationToken)
            : 0;

        var basket = ToBasket(command.BasketTemplateId, command.BasketQuantity);

        var withdrawalResult = point.RegisterWithdrawal(
            beneficiary, command.Categories, alreadyInCycle, occurredAt, command.Notes, basket);

        if (withdrawalResult.IsError)
        {
            return withdrawalResult.Errors;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return withdrawalResult.Value.ToResponse();
    }

    private static (BasketTemplateId Id, int Quantity)? ToBasket(Guid? basketTemplateId, int? basketQuantity) =>
        basketTemplateId is null ? null : (BasketTemplateId.From(basketTemplateId.Value), basketQuantity!.Value);
}
