using DoacaoApp.Domain.Errors;
using ErrorOr;

namespace DoacaoApp.Domain.ValueObjects;

/// <summary>Periodicidade do ciclo de retiradas de um ponto de coleta.</summary>
public enum CycleType
{
    Weekly = 1,
    Biweekly = 2,
    Monthly = 3,
    Annual = 4,
}

/// <summary>
/// Define a regra de retirada de um ponto de coleta: quantas retiradas são permitidas
/// por ciclo e qual a duração do ciclo. Sabe calcular a janela [início, fim) do
/// ciclo vigente para uma data — usado para contar retiradas em TODOS os pontos.
/// </summary>
public sealed record WithdrawalCycle
{
    private const int DefaultMaxWithdrawalsPerCycle = 3;

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

    /// <summary>Regra padrão de um ponto novo: 3 retiradas por ano.</summary>
    public static WithdrawalCycle AnnualDefault() => new(CycleType.Annual, DefaultMaxWithdrawalsPerCycle);

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
            CycleType.Annual => AnnualPeriod(reference),
            _ => MonthlyPeriod(reference),
        };
    }

    private static (DateTime, DateTime) MonthlyPeriod(DateTime reference)
    {
        var start = new DateTime(reference.Year, reference.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1));
    }

    private static (DateTime, DateTime) AnnualPeriod(DateTime reference)
    {
        var start = new DateTime(reference.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddYears(1));
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
        var diff = (7 + (int)reference.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return reference.Date.AddDays(-diff);
    }
}