/** Categorias do patrocínio mensal, do maior para o menor valor. */
export type SponsorTier = 'Diamond' | 'Gold' | 'Silver'

export const sponsorTiers: SponsorTier[] = ['Diamond', 'Gold', 'Silver']

export const sponsorTierLabels: Record<SponsorTier, string> = {
  Diamond: 'Diamante',
  Gold: 'Ouro',
  Silver: 'Prata',
}

/** SponsorResponse do backend. */
export interface Sponsor {
  id: string
  name: string
  tier: SponsorTier
  websiteUrl: string | null
  logoUrl: string | null
  isActive: boolean
}

export interface CreateSponsorInput {
  name: string
  tier: SponsorTier
  websiteUrl?: string | null
  logoFileId: string
}

export interface UpdateSponsorInput {
  name: string
  tier: SponsorTier
  websiteUrl?: string | null
  isActive: boolean
  logoFileId?: string
}
