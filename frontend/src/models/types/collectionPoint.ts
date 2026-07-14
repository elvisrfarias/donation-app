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
  Food: 'Alimentos',
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
  /** Nº de famílias distintas já atendidas no ponto (indicador de impacto). */
  familiesServed: number
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
