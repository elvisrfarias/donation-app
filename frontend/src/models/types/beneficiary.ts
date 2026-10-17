export type CycleType = 'Weekly' | 'Biweekly' | 'Monthly' | 'Annual'

export interface Cycle {
  type: CycleType
  maxWithdrawalsPerCycle: number
}

/**
 * Tipo de parentesco entre dois beneficiários (padrão de cadastro social, ex.: CadÚnico).
 * Os vínculos são apenas informativos: não alteram a cota de retiradas de ninguém.
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

/** Endereço de residência do beneficiário (CEP formatado). */
export interface BeneficiaryAddress {
  street: string
  number: string
  complement?: string | null
  district: string
  city: string
  state: string
  zipCode: string
}

/** BeneficiaryResponse do backend. */
export interface Beneficiary {
  id: string
  name: string
  cpf: string
  birthDate?: string | null
  /** Obrigatório no cadastro; nulo só em cadastros antigos. */
  email?: string | null
  /** Até 2 telefones de contato (formatados). */
  phones: string[]
  /** Opcional. */
  address?: BeneficiaryAddress | null
  /** Pontos que atendem a pessoa — só eles a enxergam e editam. */
  linkedPointIds: string[]
  isActive: boolean
  kinships: BeneficiaryKinship[]
}

/**
 * BeneficiaryCpfLookupResponse do backend (POST /beneficiaries/lookup-by-cpf). Quem já tem acesso
 * ao cadastro (Admin ou ponto vinculado) recebe o registro completo em `beneficiary`; os demais
 * recebem só o nome abreviado ("Maria S."), o CPF mascarado e se a pessoa está ativa.
 */
export interface BeneficiaryCpfLookup {
  id: string
  name: string
  cpf: string
  isActive: boolean
  /** Vínculo com o ponto do gestor logado (sempre `false` para o Admin, que não tem ponto). */
  isLinkedToMyPoint: boolean
  beneficiary: Beneficiary | null
}

/** Se o resultado da busca por CPF já está vinculado ao ponto informado. */
export function isLookupLinkedTo(lookup: BeneficiaryCpfLookup, pointId: string): boolean {
  return lookup.beneficiary?.linkedPointIds.includes(pointId) ?? false
}

export interface RegisterBeneficiaryInput {
  name: string
  cpf: string
  email: string
  phones: string[]
  address?: BeneficiaryAddress | null
  birthDate: string
  registeredByPointId: string
}

export type UpdateBeneficiaryInput = Omit<RegisterBeneficiaryInput, 'registeredByPointId'>

export interface AddBeneficiaryKinshipInput {
  relatedBeneficiaryCpf: string
  type: KinshipType
}

export const cycleTypeLabels: Record<CycleType, string> = {
  Weekly: 'Semanal',
  Biweekly: 'Quinzenal',
  Monthly: 'Mensal',
  Annual: 'Anual',
}
