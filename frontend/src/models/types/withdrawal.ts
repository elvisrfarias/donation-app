/** WithdrawalResponse do backend. */
export interface Withdrawal {
  id: string
  pointId: string
  familyId: string
  beneficiaryId: string
  occurredAtUtc: string
  notes?: string | null
}

/** Corpo do RegisterWithdrawalCommand. */
export interface RegisterWithdrawalInput {
  pointId: string
  familyId: string
  beneficiaryId: string
  occurredAtUtc?: string | null
  notes?: string | null
}

/** FamilyWithdrawalHistoryResponse — histórico municipal + saldo do ciclo vigente. */
export interface FamilyWithdrawalHistory {
  familyId: string
  familyName: string
  cycleType: string
  maxPerCycle: number
  usedInCurrentCycle: number
  remainingInCurrentCycle: number
  currentCycleStartUtc: string
  currentCycleEndUtc: string
  withdrawals: Withdrawal[]
}
