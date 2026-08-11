export type CycleType = 'Weekly' | 'Biweekly' | 'Monthly'

export interface Cycle {
  type: CycleType
  maxWithdrawalsPerCycle: number
}

/**
 * Tipo de parentesco entre dois beneficiários (padrão de cadastro social, ex.: CadÚnico).
 * Só "Conjuge" entre beneficiários de famílias diferentes forma uma família nova; os
 * demais tipos são só informativos.
 */
export type KinshipType =
  | 'Conjuge'
  | 'Pai'
  | 'Filho'
  | 'Irmao'
  | 'Avo'
  | 'Neto'
  | 'Tio'
  | 'Sobrinho'
  | 'Primo'
  | 'Sogro'
  | 'GenroOuNora'
  | 'Agregado'
  | 'Outro'

export const kinshipTypeLabels: Record<KinshipType, string> = {
  Conjuge: 'Cônjuge / Companheiro(a)',
  Pai: 'Pai / Mãe',
  Filho: 'Filho(a) / Enteado(a)',
  Irmao: 'Irmão / Irmã',
  Avo: 'Avô / Avó',
  Neto: 'Neto(a) / Bisneto(a)',
  Tio: 'Tio / Tia',
  Sobrinho: 'Sobrinho(a)',
  Primo: 'Primo(a)',
  Sogro: 'Sogro(a)',
  GenroOuNora: 'Genro / Nora',
  Agregado: 'Agregado(a) (mora junto, sem parentesco)',
  Outro: 'Outro parentesco',
}

/** BeneficiaryKinshipResponse do backend. */
export interface BeneficiaryKinship {
  relatedBeneficiaryId: string
  relatedBeneficiaryName: string
  relatedBeneficiaryCpf: string
  type: KinshipType
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
  kinships: BeneficiaryKinship[]
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

export type UpdateBeneficiaryInput = RegisterBeneficiaryInput

export interface AddBeneficiaryKinshipInput {
  relatedBeneficiaryCpf: string
  type: KinshipType
}

export const cycleTypeLabels: Record<CycleType, string> = {
  Weekly: 'Semanal',
  Biweekly: 'Quinzenal',
  Monthly: 'Mensal',
}
