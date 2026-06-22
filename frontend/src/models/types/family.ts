export type CycleType = 'Weekly' | 'Biweekly' | 'Monthly'

export interface Cycle {
  type: CycleType
  maxWithdrawalsPerCycle: number
}

/** BeneficiaryResponse do backend. */
export interface Beneficiary {
  id: string
  familyId: string
  name: string
  cpf: string
  birthDate?: string | null
  registeredByPointId: string
  isActive: boolean
}

/** FamilyResponse do backend. */
export interface Family {
  id: string
  name: string
  cycle: Cycle
  beneficiaries: Beneficiary[]
}

export interface CreateFamilyInput {
  name: string
  cycleType: CycleType
  maxWithdrawalsPerCycle: number
}

export interface RegisterBeneficiaryInput {
  name: string
  cpf: string
  birthDate?: string | null
  registeredByPointId: string
}

export const cycleTypeLabels: Record<CycleType, string> = {
  Weekly: 'Semanal',
  Biweekly: 'Quinzenal',
  Monthly: 'Mensal',
}
