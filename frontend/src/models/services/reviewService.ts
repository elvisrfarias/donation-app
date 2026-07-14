import { http } from '@/models/api/httpClient'
import type { CreateReviewInput, Review } from '@/models/types'

export const reviewService = {
  /** Lista pública (anônima) das avaliações de um ponto de coleta. */
  async listByPoint(pointId: string, signal?: AbortSignal): Promise<Review[]> {
    const { data } = await http.get<Review[]>(
      `/public/collection-points/${pointId}/reviews`,
      { signal },
    )
    return data
  },

  /** Registra uma avaliação da comunidade para um ponto (anônimo). */
  async create(pointId: string, input: CreateReviewInput): Promise<Review> {
    const { data } = await http.post<Review>(
      `/public/collection-points/${pointId}/reviews`,
      input,
    )
    return data
  },

  /** Denuncia uma avaliação inadequada para moderação (anônimo). */
  async report(reviewId: string): Promise<void> {
    await http.post(`/public/reviews/${reviewId}/report`)
  },
}
