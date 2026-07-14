/** Avaliação da comunidade sobre um ponto de coleta (espelha ReviewResponse do backend). */
export interface Review {
  id: string
  collectionPointId: string
  authorName: string
  /** Nota de 1 a 5 estrelas. */
  rating: number
  comment?: string | null
  createdAtUtc: string
}

/** Corpo do POST de avaliação (o ponto vem da rota). */
export interface CreateReviewInput {
  authorName: string
  rating: number
  comment?: string | null
}

export const MIN_RATING = 1
export const MAX_RATING = 5
