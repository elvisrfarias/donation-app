import { http } from '@/models/api/httpClient'
import type {
  AddBeneficiaryKinshipInput,
  Beneficiary,
  BeneficiaryWithdrawalHistory,
  RegisterBeneficiaryInput,
  UpdateBeneficiaryInput,
} from '@/models/types'

export const beneficiaryService = {
  async list(signal?: AbortSignal): Promise<Beneficiary[]> {
    const { data } = await http.get<Beneficiary[]>('/beneficiaries', { signal })
    return data
  },

  async getById(id: string): Promise<Beneficiary> {
    const { data } = await http.get<Beneficiary>(`/beneficiaries/${id}`)
    return data
  },

  /** Busca rápida do beneficiário pelo CPF, no município inteiro (usada no registro de retirada). */
  async findByCpf(cpf: string): Promise<Beneficiary> {
    const digits = cpf.replace(/\D/g, '')
    const { data } = await http.get<Beneficiary>(`/beneficiaries/by-cpf/${digits}`)
    return data
  },

  async register(input: RegisterBeneficiaryInput): Promise<Beneficiary> {
    const { data } = await http.post<Beneficiary>('/beneficiaries', input)
    return data
  },

  async update(beneficiaryId: string, input: UpdateBeneficiaryInput): Promise<Beneficiary> {
    const { data } = await http.put<Beneficiary>(`/beneficiaries/${beneficiaryId}`, input)
    return data
  },

  async addKinship(beneficiaryId: string, input: AddBeneficiaryKinshipInput): Promise<Beneficiary> {
    const { data } = await http.post<Beneficiary>(`/beneficiaries/${beneficiaryId}/kinships`, input)
    return data
  },

  /** Histórico municipal + saldo do ciclo segundo a regra do ponto informado. */
  async getWithdrawalHistory(
    beneficiaryId: string,
    pointId: string,
  ): Promise<BeneficiaryWithdrawalHistory> {
    const { data } = await http.get<BeneficiaryWithdrawalHistory>(
      `/beneficiaries/${beneficiaryId}/withdrawals`,
      { params: { pointId } },
    )
    return data
  },
}
