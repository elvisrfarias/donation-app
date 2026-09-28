import type { WithdrawalItemRef } from './collectionPoint'

/** Um item do corpo de Register/UpdateWithdrawalCommand: item de estoque do ponto e quantidade dada. */
export interface WithdrawalItemEntryInput {
  stockItemId: string
  quantity: number
}

/** WithdrawalResponse do backend. */
export interface Withdrawal {
  id: string
  pointId: string
  beneficiaryId: string
  /** Itens específicos dados na visita, com quantidade — sem limite nem baixa no ciclo (só a
   *  cesta básica conta para o limite), mas cada um desconta do estoque do ponto. Já vêm
   *  denormalizados (nome/unidade/categoria) pelo backend. */
  items: WithdrawalItemRef[]
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
  items: WithdrawalItemEntryInput[]
  occurredAtUtc?: string | null
  notes?: string | null
  /** Receita de cesta básica retirada nesta visita, se houver — sujeita ao limite do ciclo e dá baixa no estoque. */
  basketTemplateId?: string | null
  basketQuantity?: number | null
}

/** Corpo do UpdateWithdrawalCommand. Só a retirada mais recente do beneficiário pode ser editada. */
export interface UpdateWithdrawalInput {
  pointId: string
  items: WithdrawalItemEntryInput[]
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
