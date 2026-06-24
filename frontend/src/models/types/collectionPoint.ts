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
  address: Address
  contact: Contact
  isActive: boolean
  createdAtUtc: string
  /** Categorias de itens declaradas pelo ponto (ex.: Food, Clothing). */
  acceptedItems: ItemCategory[]
  /** URL (temporária) da foto do ponto. Nulo nos pontos antigos/seed — a UI cai no ícone. */
  imageUrl?: string | null
}

/** Corpo do CreateCollectionPointCommand. */
export interface CreateCollectionPointInput {
  name: string
  type: CollectionPointType
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
}

/** Corpo do UpdateCollectionPointCommand. A imagem é opcional: envie `imageFileId`
 * só quando quiser trocar a foto; ausente/nulo mantém a atual. */
export interface UpdateCollectionPointInput {
  name: string
  type: CollectionPointType
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
