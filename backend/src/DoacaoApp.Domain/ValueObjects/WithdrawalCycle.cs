using DoacaoApp.Domain.Errors;
using ErrorOr;

namespace DoacaoApp.Domain.ValueObjects;

/// <summary>Periodicidade do ciclo de retiradas de uma família.</summary>
public enum CycleType
{
    Weekly = 1,
    Biweekly = 2,
    Monthly = 3,
}

/// <summary>
/// Define a regra de equidade de uma família: quantas retiradas são permitidas
/// por ciclo e qual a duração do ciclo. Sabe calcular a janela [início, fim) do
/// ciclo vigente para uma data — usado para contar retiradas em TODOS os pontos.
/// </summary>
public sealed record WithdrawalCycle
{
    // Segunda-feira, usada como âncora para o ciclo quinzenal.
    private static readonly DateTime BiweeklyEpoch = new(2000, 1, 3, 0, 0, 0, DateTimeKind.Utc);

    private WithdrawalCycle(CycleType type, int maxWithdrawalsPerCycle)
    {
        Type = type;
        MaxWithdrawalsPerCycle = maxWithdrawalsPerCycle;
    }

    public CycleType Type { get; }

    /// <summary>Máximo de retiradas permitidas dentro de um ciclo.</summary>
    public int MaxWithdrawalsPerCycle { get; }

    public static ErrorOr<WithdrawalCycle> Create(CycleType type, int maxWithdrawalsPerCycle)
    {
        if (maxWithdrawalsPerCycle < 1)
        {
            return DomainErrors.WithdrawalCycle.InvalidMax;
        }

        return new WithdrawalCycle(type, maxWithdrawalsPerCycle);
    }

    /// <summary>Atalho mais comum: 1 retirada por mês.</summary>
    public static WithdrawalCycle MonthlyDefault() => new(CycleType.Monthly, 1);

    /// <summary>
    /// Retorna a janela do ciclo vigente para <paramref name="referenceUtc"/>:
    /// início inclusivo e fim exclusivo, ambos em UTC.
    /// </summary>
    public (DateTime StartInclusive, DateTime EndExclusive) GetCurrentPeriod(DateTime referenceUtc)
    {
        var reference = DateTime.SpecifyKind(referenceUtc, DateTimeKind.Utc);

        return Type switch
        {
            CycleType.Monthly => MonthlyPeriod(reference),
            CycleType.Weekly => WeeklyPeriod(reference),
            CycleType.Biweekly => BiweeklyPeriod(reference),
            _ => MonthlyPeriod(reference),
        };
    }

    private static (DateTime, DateTime) MonthlyPeriod(DateTime reference)
    {
        var start = new DateTime(reference.Year, reference.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1));
    }

    private static (DateTime, DateTime) WeeklyPeriod(DateTime reference)
    {
        var start = StartOfWeek(reference);
        return (start, start.AddDays(7));
    }

    private static (DateTime, DateTime) BiweeklyPeriod(DateTime reference)
    {
        var daysSinceEpoch = (reference.Date - BiweeklyEpoch.Date).Days;
        var blockStartDays = (daysSinceEpoch / 14) * 14;
        var start = BiweeklyEpoch.AddDays(blockStartDays);
        return (start, start.AddDays(14));
    }

    private static DateTime StartOfWeek(DateTime reference)
    {
        // Segunda-feira como início da semana.
        var diff = (7 + (int)reference.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return reference.Date.AddDays(-diff);
    }
}