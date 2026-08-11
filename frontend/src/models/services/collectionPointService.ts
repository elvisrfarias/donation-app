import { http } from '@/models/api/httpClient'
import type {
  CollectionPoint,
  CreateCollectionPointInput,
  StockItem,
  UpdateCollectionPointInput,
  UploadedImage,
} from '@/models/types'

export const collectionPointService = {
  /** Lista pública (anônima) dos pontos de coleta ativos. Use sort='top-rated' para o ranking. */
  async listPublic(
    signal?: AbortSignal,
    sort?: 'top-rated',
  ): Promise<CollectionPoint[]> {
    const { data } = await http.get<CollectionPoint[]>('/public/collection-points', {
      signal,
      params: sort ? { sort } : undefined,
    })
    return data
  },

  /** Envia a imagem do ponto (visibilidade pública) e devolve o id/URL do arquivo. */
  async uploadImage(file: File): Promise<UploadedImage> {
    const form = new FormData()
    form.append('file', file)
    form.append('visibility', 'Public')
    form.append('ownerType', 'CollectionPoint')
    const { data } = await http.post<UploadedImage>('/files', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data
  },

  /** Lista administrativa (autenticada) de todos os pontos, ativos e inativos (requer role Admin). */
  async listForManagement(signal?: AbortSignal): Promise<CollectionPoint[]> {
    const { data } = await http.get<CollectionPoint[]>('/collection-points', { signal })
    return data
  },

  /** Cadastra um novo ponto de coleta (requer role Admin). */
  async create(input: CreateCollectionPointInput): Promise<CollectionPoint> {
    const { data } = await http.post<CollectionPoint>('/collection-points', input)
    return data
  },

  /** Atualiza um ponto de coleta existente (requer role Admin). */
  async update(id: string, input: UpdateCollectionPointInput): Promise<CollectionPoint> {
    const { data } = await http.put<CollectionPoint>(`/collection-points/${id}`, input)
    return data
  },

  /** Redefine o estoque (itens disponíveis) do ponto (requer role Admin ou PointManager). */
  async updateStock(id: string, items: StockItem[]): Promise<CollectionPoint> {
    const { data } = await http.put<CollectionPoint>(`/collection-points/${id}/stock`, { items })
    return data
  },
}
