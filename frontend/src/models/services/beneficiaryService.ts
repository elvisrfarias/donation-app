import { http } from '@/models/api/httpClient'
import type {
  AddBeneficiaryKinshipInput,
  Beneficiary,
  BeneficiaryCpfLookup,
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

  /**
   * Busca rápida do beneficiário pelo CPF, no município inteiro (balcão de retirada e cadastro).
   * O CPF vai no corpo do POST para não aparecer em URLs/logs de acesso; sem vínculo com o ponto,
   * o backend devolve só a projeção mascarada.
   */
  async lookupByCpf(cpf: string): Promise<BeneficiaryCpfLookup> {
    const digits = cpf.replace(/\D/g, '')
    const { data } = await http.post<BeneficiaryCpfLookup>('/beneficiaries/lookup-by-cpf', {
      cpf: digits,
    })
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

  /** Vincula um beneficiário já cadastrado a um ponto (decisão do gestor do ponto). */
  async linkToPoint(beneficiaryId: string, pointId: string): Promise<Beneficiary> {
    const { data } = await http.post<Beneficiary>(`/beneficiaries/${beneficiaryId}/points`, {
      pointId,
    })
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
