import type { ItemCategory } from './collectionPoint'

/** WithdrawalResponse do backend. */
export interface Withdrawal {
  id: string
  pointId: string
  beneficiaryId: string
  /** Itens retirados na visita (roupas, calçados… — sem limite nem baixa de estoque). */
  categories: ItemCategory[]
  /** Receita de cesta básica retirada nesta visita, se houver. */
  basketTemplateId?: string | null
  basketQuantity?: number | null
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
  /** Receita de cesta básica retirada nesta visita, se houver — sujeita ao limite do ciclo e dá baixa no estoque. */
  basketTemplateId?: string | null
  basketQuantity?: number | null
}

/** Corpo do UpdateWithdrawalCommand. Só a retirada mais recente do beneficiário pode ser editada. */
export interface UpdateWithdrawalInput {
  pointId: string
  categories: ItemCategory[]
  notes?: string | null
  basketTemplateId?: string | null
  basketQuantity?: number | null
}

/** BeneficiaryWithdrawalHistoryResponse — histórico municipal + saldo do ciclo vigente. */
export interface BeneficiaryWithdrawalHistory {
  beneficiaryId: string
  beneficiaryName: string
  /**
   * Vem sempre com o valor fixo "Basket": o limite do ciclo não é mais amarrado a uma categoria de
   * item — é "levar cesta básica ou não" (ver `BeneficiaryWithdrawalHistoryResponse` no backend).
   */
  limitedCategory: string
  cycleType: string
  maxPerCycle: number
  usedInCurrentCycle: number
  remainingInCurrentCycle: number
  currentCycleStartUtc: string
  currentCycleEndUtc: string
  withdrawals: Withdrawal[]
}
