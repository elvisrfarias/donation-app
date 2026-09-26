namespace DoacaoApp.Application.CollectionPoints.Contracts;

/// <summary>
/// Relatório do histórico municipal de retiradas de um beneficiário, com o saldo do ciclo vigente.
/// </summary>
public sealed record BeneficiaryWithdrawalHistoryResponse(
    Guid BeneficiaryId,
    string BeneficiaryName,
    string LimitedCategory,
    string CycleType,
    int MaxPerCycle,
    int UsedInCurrentCycle,
    int RemainingInCurrentCycle,
    DateTime CurrentCycleStartUtc,
    DateTime CurrentCycleEndUtc,
    IReadOnlyList<WithdrawalResponse> Withdrawals);
