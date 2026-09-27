import type { ItemCategory } from './collectionPoint'

/** WithdrawalResponse do backend. */
export interface Withdrawal {
  id: string
  pointId: string
  beneficiaryId: string
  /** Itens retirados na visita (cestas básicas, roupas, calçados…). */
  categories: ItemCategory[]
  occurredAtUtc: string
  notes?: string | null
}

/** Corpo do RegisterWithdrawalCommand. */
export interface RegisterWithdrawalInput {
  pointId: string
  beneficiaryId: string
  categories: ItemCategory[]
  occurredAtUtc?: string | null
  notes?: string | null
}

/** Corpo do UpdateWithdrawalCommand. Só a retirada mais recente do beneficiário pode ser editada. */
export interface UpdateWithdrawalInput {
  pointId: string
  categories: ItemCategory[]
  notes?: string | null
}

/** BeneficiaryWithdrawalHistoryResponse — histórico municipal + saldo do ciclo vigente. */
export interface BeneficiaryWithdrawalHistory {
  beneficiaryId: string
  beneficiaryName: string
  /** Única categoria sujeita ao limite por ciclo (as demais são livres, mas registradas). */
  limitedCategory: ItemCategory
  cycleType: string
  maxPerCycle: number
  usedInCurrentCycle: number
  remainingInCurrentCycle: number
  currentCycleStartUtc: string
  currentCycleEndUtc: string
  withdrawals: Withdrawal[]
}
