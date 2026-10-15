export type DonationStatus = 'Pledged' | 'Received' | 'Cancelled'

/** Doação prometida a um ponto de coleta (espelha DonationResponse do backend). */
export interface Donation {
  id: string
  collectionPointId: string
  donorName: string
  donorPhone?: string | null
  description: string
  status: DonationStatus
  createdAtUtc: string
  closedAtUtc?: string | null
}

/** Corpo do POST público de doação prometida (o ponto vem da rota). */
export interface PledgeDonationInput {
  donorName: string
  donorPhone?: string | null
  description: string
}

export interface ReceivedStockEntry {
  stockItemId: string
  quantity: number
}

/** Corpo do recebimento: itens de estoque a somar (opcional). */
export interface ReceiveDonationInput {
  receivedStock?: ReceivedStockEntry[]
}

export const donationStatusLabels: Record<DonationStatus, string> = {
  Pledged: 'Aguardando entrega',
  Received: 'Recebida',
  Cancelled: 'Cancelada',
}

export const MAX_DONATION_DESCRIPTION_LENGTH = 500
export const MAX_DONOR_NAME_LENGTH = 120
