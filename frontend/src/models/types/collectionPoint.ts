export type CollectionPointType = 'Association' | 'Church' | 'PublicEntity'

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
  /** Id do arquivo de imagem já enviado (POST /api/files). Obrigatório. */
  imageFileId: string
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
