namespace DoacaoApp.Application.CollectionPoints.Contracts;

/// <summary>
/// Relatório do histórico municipal de retiradas de um beneficiário, com o saldo do ciclo vigente.
/// <see cref="LimitedCategory"/> vem com o valor fixo <c>"Basket"</c>: o limite do ciclo não é mais
/// amarrado a uma categoria de item — é "levar cesta básica ou não" (ver
/// <c>Withdrawal.CountsTowardsLimit</c> no domínio).
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
