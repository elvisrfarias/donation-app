namespace DoacaoApp.Application.CollectionPoints.Contracts;

/// <summary>
/// Relatório do histórico municipal de retiradas de uma família, com o saldo do ciclo vigente.
/// </summary>
public sealed record FamilyWithdrawalHistoryResponse(
    Guid FamilyId,
    string FamilyName,
    string CycleType,
    int MaxPerCycle,
    int UsedInCurrentCycle,
    int RemainingInCurrentCycle,
    DateTime CurrentCycleStartUtc,
    DateTime CurrentCycleEndUtc,
    IReadOnlyList<WithdrawalResponse> Withdrawals);