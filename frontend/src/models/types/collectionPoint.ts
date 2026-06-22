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
