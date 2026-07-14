import { http } from '@/models/api/httpClient'
import type {
  Beneficiary,
  CreateFamilyInput,
  Family,
  FamilyWithdrawalHistory,
  RegisterBeneficiaryInput,
  UpdateBeneficiaryInput,
} from '@/models/types'

export const familyService = {
  async create(input: CreateFamilyInput): Promise<Family> {
    const { data } = await http.post<Family>('/families', input)
    return data
  },

  async list(signal?: AbortSignal): Promise<Family[]> {
    const { data } = await http.get<Family[]>('/families', { signal })
    return data
  },

  async getById(id: string): Promise<Family> {
    const { data } = await http.get<Family>(`/families/${id}`)
    return data
  },

  /** Busca rápida da família pelo CPF de um beneficiário (usada no registro de retirada). */
  async findByCpf(cpf: string): Promise<Family> {
    const digits = cpf.replace(/\D/g, '')
    const { data } = await http.get<Family>(`/families/by-beneficiary-cpf/${digits}`)
    return data
  },

  async registerBeneficiary(
    familyId: string,
    input: RegisterBeneficiaryInput,
  ): Promise<Beneficiary> {
    const { data } = await http.post<Beneficiary>(`/families/${familyId}/beneficiaries`, input)
    return data
  },

  async updateBeneficiary(
    familyId: string,
    beneficiaryId: string,
    input: UpdateBeneficiaryInput,
  ): Promise<Beneficiary> {
    const { data } = await http.put<Beneficiary>(
      `/families/${familyId}/beneficiaries/${beneficiaryId}`,
      input,
    )
    return data
  },

  async getWithdrawalHistory(familyId: string): Promise<FamilyWithdrawalHistory> {
    const { data } = await http.get<FamilyWithdrawalHistory>(`/families/${familyId}/withdrawals`)
    return data
  },
}
