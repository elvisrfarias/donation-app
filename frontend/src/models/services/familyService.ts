import { http } from '@/models/api/httpClient'
import type {
  Beneficiary,
  CreateFamilyInput,
  Family,
  FamilyWithdrawalHistory,
  RegisterBeneficiaryInput,
} from '@/models/types'

export const familyService = {
  async create(input: CreateFamilyInput): Promise<Family> {
    const { data } = await http.post<Family>('/families', input)
    return data
  },

  async getById(id: string): Promise<Family> {
    const { data } = await http.get<Family>(`/families/${id}`)
    return data
  },

  async registerBeneficiary(
    familyId: string,
    input: RegisterBeneficiaryInput,
  ): Promise<Beneficiary> {
    const { data } = await http.post<Beneficiary>(`/families/${familyId}/beneficiaries`, input)
    return data
  },

  async getWithdrawalHistory(familyId: string): Promise<FamilyWithdrawalHistory> {
    const { data } = await http.get<FamilyWithdrawalHistory>(`/families/${familyId}/withdrawals`)
    return data
  },
}
