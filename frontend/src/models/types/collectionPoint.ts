import type { Cycle, CycleType } from './beneficiary'

export type CollectionPointType = 'Association' | 'Church' | 'PublicEntity'

/** Categorias de itens que um ponto recebe/disponibiliza (espelha o enum ItemCategory do backend). */
export type ItemCategory =
  | 'Food'
  | 'Clothing'
  | 'Footwear'
  | 'Hygiene'
  | 'Cleaning'
  | 'Furniture'
  | 'Appliances'
  | 'Toys'
  | 'Books'
  | 'Medicine'
  | 'Other'

export const itemCategoryValues: ItemCategory[] = [
  'Food',
  'Clothing',
  'Footwear',
  'Hygiene',
  'Cleaning',
  'Furniture',
  'Appliances',
  'Toys',
  'Books',
  'Medicine',
  'Other',
]

export const itemCategoryLabels: Record<ItemCategory, string> = {
  Food: 'Cestas básicas',
  Clothing: 'Roupas',
  Footwear: 'Calçados',
  Hygiene: 'Higiene',
  Cleaning: 'Limpeza',
  Furniture: 'Móveis',
  Appliances: 'Eletrodomésticos',
  Toys: 'Brinquedos',
  Books: 'Livros',
  Medicine: 'Medicamentos',
  Other: 'Outros',
}

export interface Address {
  street: string
  number: string
  complement?: string | null
  district: string
  city: string
  state: string
  zipCode: string
  /** Coordenadas fixadas no mapa; nulas nos pontos ainda não geolocalizados. */
  latitude?: number | null
  longitude?: number | null
}

export interface Contact {
  responsibleName: string
  email: string
  phone: string
}

/** Uma linha do estoque de um ponto: categoria e quantidade disponível. */
export interface StockItem {
  category: ItemCategory
  quantity: number
}

/**
 * Configuração de estoque de uma categoria: meta desejada (0 = sem meta), quantidade a partir da qual
 * o estoque é considerado baixo (aviso) e se a categoria é acompanhada pelo ponto.
 */
export interface StockSetting {
  category: ItemCategory
  goal: number
  alertThreshold: number
  isTracked: boolean
}

/** Corpo do UpdateCollectionPointStockSettingsCommand. */
export interface UpdateStockSettingsInput {
  items: StockSetting[]
  cycleType: CycleType
  maxWithdrawalsPerCycle: number
}

/** CollectionPointResponse do backend. */
export interface CollectionPoint {
  id: string
  name: string
  type: CollectionPointType
  /** Texto livre que apresenta a iniciativa/projeto (opcional). */
  description?: string | null
  /** Horário de funcionamento em texto livre (ex.: "Seg a Sex, 8h–17h"). */
  openingHours?: string | null
  /** Itens que o ponto precisa com urgência agora. */
  needs: string[]
  /** Itens disponíveis no ponto agora (categoria → quantidade). */
  stock: StockItem[]
  /** Metas e avisos por categoria. Só vem nas consultas de gestão (vazio na lista pública). */
  stockSettings: StockSetting[]
  /** Regra de retirada do ponto: ciclo + máximo por família em cada ciclo. */
  withdrawalRule: Cycle
  address: Address
  contact: Contact
  isActive: boolean
  createdAtUtc: string
  /** Categorias de itens declaradas pelo ponto (ex.: Food, Clothing). */
  acceptedItems: ItemCategory[]
  /** URL (temporária) da foto do ponto. Nulo nos pontos antigos/seed — a UI cai no ícone. */
  imageUrl?: string | null
  /** Média das notas (0 quando ainda não há avaliações). */
  averageRating: number
  /** Total de avaliações registradas pela comunidade. */
  reviewCount: number
  /** Nº de beneficiários distintos já atendidos no ponto (indicador de impacto). */
  beneficiariesServed: number
}

export type DashboardGranularity = 'Day' | 'Month'

/** Retiradas de um dia ou mês; `date` é o primeiro dia do intervalo (yyyy-MM-dd). */
export interface WithdrawalsSeriesPoint {
  date: string
  count: number
}

export interface RecentWithdrawal {
  withdrawalId: string
  beneficiaryName: string
  categories: ItemCategory[]
  occurredAtUtc: string
}

/** Indicadores de resultado de um único ponto em um período (painel do gestor). */
export interface CollectionPointDashboard {
  pointId: string
  periodStart: string
  periodEnd: string
  withdrawalsInPeriod: number
  beneficiariesServedInPeriod: number
  totalWithdrawals: number
  registeredBeneficiaries: number
  granularity: DashboardGranularity
  withdrawalsSeries: WithdrawalsSeriesPoint[]
  recentWithdrawals: RecentWithdrawal[]
}

/** Corpo do CreateCollectionPointCommand. */
export interface CreateCollectionPointInput {
  name: string
  type: CollectionPointType
  description?: string | null
  openingHours?: string | null
  needs?: string[]
  street: string
  number: string
  complement?: string | null
  district: string
  city: string
  state: string
  zipCode: string
  responsibleName: string
  email: string
  phone: string
  /** Categorias de itens do ponto (ao menos uma). */
  acceptedItems: ItemCategory[]
  /** Id do arquivo de imagem já enviado (POST /api/files). Obrigatório. */
  imageFileId: string
  /** Coordenadas exatas do ponto no mapa (opcionais). */
  latitude?: number | null
  longitude?: number | null
}

/** Corpo do UpdateCollectionPointCommand. A imagem é opcional: envie `imageFileId`
 * só quando quiser trocar a foto; ausente/nulo mantém a atual. */
export interface UpdateCollectionPointInput {
  name: string
  type: CollectionPointType
  description?: string | null
  openingHours?: string | null
  needs?: string[]
  street: string
  number: string
  complement?: string | null
  district: string
  city: string
  state: string
  zipCode: string
  responsibleName: string
  email: string
  phone: string
  acceptedItems: ItemCategory[]
  /** Id da nova imagem (POST /api/files). Omitido mantém a imagem atual. */
  imageFileId?: string | null
  /** Coordenadas exatas do ponto no mapa (opcionais). */
  latitude?: number | null
  longitude?: number | null
}

/** Metadados de um arquivo enviado ao storage (subconjunto do StoredFileResponse). */
export interface UploadedImage {
  id: string
  url?: string | null
}

export const collectionPointTypeLabels: Record<CollectionPointType, string> = {
  Association: 'Associação',
  Church: 'Igreja',
  PublicEntity: 'Entidade Pública',
}

/** Monta a string de endereço usada para geocodificação e exibição. */
export function formatAddress(address: Address): string {
  const parts = [
    `${address.street}, ${address.number}`,
    address.district,
    `${address.city} - ${address.state}`,
    address.zipCode,
  ]
  return parts.filter(Boolean).join(', ')
}
