/** WithdrawalResponse do backend. */
export interface Withdrawal {
  id: string
  pointId: string
  beneficiaryId: string
  occurredAtUtc: string
  notes?: string | null
}

/** Corpo do RegisterWithdrawalCommand. */
export interface RegisterWithdrawalInput {
  pointId: string
  beneficiaryId: string
  occurredAtUtc?: string | null
  notes?: string | null
}

/** BeneficiaryWithdrawalHistoryResponse — histórico municipal + saldo do ciclo vigente. */
export interface BeneficiaryWithdrawalHistory {
  beneficiaryId: string
  beneficiaryName: string
  cycleType: string
  maxPerCycle: number
  usedInCurrentCycle: number
  remainingInCurrentCycle: number
  currentCycleStartUtc: string
  currentCycleEndUtc: string
  withdrawals: Withdrawal[]
}
