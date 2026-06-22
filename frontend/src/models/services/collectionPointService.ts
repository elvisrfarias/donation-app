import { http } from '@/models/api/httpClient'
import type { CollectionPoint, CreateCollectionPointInput } from '@/models/types'

export const collectionPointService = {
  /** Lista pública (anônima) dos pontos de coleta ativos. */
  async listPublic(signal?: AbortSignal): Promise<CollectionPoint[]> {
    const { data } = await http.get<CollectionPoint[]>('/public/collection-points', { signal })
    return data
  },

  /** Cadastra um novo ponto de coleta (requer role Admin). */
  async create(input: CreateCollectionPointInput): Promise<CollectionPoint> {
    const { data } = await http.post<CollectionPoint>('/collection-points', input)
    return data
  },
}
