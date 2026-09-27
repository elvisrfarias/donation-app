import { http } from '@/models/api/httpClient'
import type { RegisterWithdrawalInput, UpdateWithdrawalInput, Withdrawal } from '@/models/types'

export const withdrawalService = {
  /** Registra uma retirada; o backend aplica a regra de equidade municipal (409 se exceder). */
  async register(input: RegisterWithdrawalInput): Promise<Withdrawal> {
    const { data } = await http.post<Withdrawal>('/withdrawals', input)
    return data
  },

  /** Corrige os itens/observações de uma retirada. Só a mais recente do beneficiário pode ser editada. */
  async update(withdrawalId: string, input: UpdateWithdrawalInput): Promise<Withdrawal> {
    const { data } = await http.put<Withdrawal>(`/withdrawals/${withdrawalId}`, input)
    return data
  },
}
