using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Families.Contracts;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Families.Commands;

/// <summary>Cria uma família com sua regra de ciclo (entidade central de rastreabilidade).</summary>
public sealed record CreateFamilyCommand(
    string Name,
    CycleType CycleType,
    int MaxWithdrawalsPerCycle) : IRequest<ErrorOr<FamilyResponse>>;

public sealed class CreateFamilyValidator : AbstractValidator<CreateFamilyCommand>
{
    public CreateFamilyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CycleType).IsInEnum();
        RuleFor(x => x.MaxWithdrawalsPerCycle).GreaterThanOrEqualTo(1);
    }
}

public sealed class CreateFamilyHandler(
    IFamilyRepository repository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<CreateFamilyCommand, ErrorOr<FamilyResponse>>
{
    public async Task<ErrorOr<FamilyResponse>> Handle(
        CreateFamilyCommand command,
        CancellationToken cancellationToken)
    {
        var cycle = WithdrawalCycle.Create(command.CycleType, command.MaxWithdrawalsPerCycle);
        if (cycle.IsError)
        {
            return cycle.Errors;
        }

        var family = Family.Create(command.Name, cycle.Value, clock.UtcNow);
        if (family.IsError)
        {
            return family.Errors;
        }

        repository.Add(family.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return family.Value.ToResponse();
    }
}