namespace DoacaoApp.Application.CollectionPoints.Contracts;

/// <summary>Unidade de tempo de cada barra da série de retiradas.</summary>
public enum DashboardGranularity
{
    Day,
    Month,
}

/// <summary>Retiradas de um ponto em um dia ou mês (<see cref="Date"/> é o primeiro dia do intervalo).</summary>
public sealed record WithdrawalsSeriesPointResponse(DateOnly Date, int Count);

/// <summary>Retirada recente de um ponto, com o nome do beneficiário atendido.</summary>
public sealed record RecentWithdrawalResponse(Guid WithdrawalId, string BeneficiaryName, DateTime OccurredAtUtc);

/// <summary>Indicadores de resultado de um único ponto de coleta em um período.</summary>
public sealed record CollectionPointDashboardResponse(
    Guid PointId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int WithdrawalsInPeriod,
    int BeneficiariesServedInPeriod,
    int TotalWithdrawals,
    int RegisteredBeneficiaries,
    DashboardGranularity Granularity,
    IReadOnlyList<WithdrawalsSeriesPointResponse> WithdrawalsSeries,
    IReadOnlyList<RecentWithdrawalResponse> RecentWithdrawals);
