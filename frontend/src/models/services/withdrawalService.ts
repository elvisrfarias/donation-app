import { http } from '@/models/api/httpClient'
import type { RegisterWithdrawalInput, Withdrawal } from '@/models/types'

export const withdrawalService = {
  /** Registra uma retirada; o backend aplica a regra de equidade municipal (409 se exceder). */
  async register(input: RegisterWithdrawalInput): Promise<Withdrawal> {
    const { data } = await http.post<Withdrawal>('/withdrawals', input)
    return data
  },
}
