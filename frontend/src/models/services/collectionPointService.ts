import { http } from '@/models/api/httpClient'
import type {
  CollectionPoint,
  CollectionPointDashboard,
  CreateBasketTemplateInput,
  CreateCategoryInput,
  CreateCollectionPointInput,
  CreateStockItemInput,
  UpdateBasketTemplateInput,
  UpdateCategoryInput,
  UpdateCollectionPointInput,
  UpdateStockItemInput,
  UpdateStockItemSettingsInput,
  UpdateStockQuantitiesInput,
  UpdateWithdrawalRuleInput,
  UploadedImage,
} from '@/models/types'

export const collectionPointService = {
  /** Lista pública (anônima) dos pontos de coleta ativos. Use sort='top-rated' para o ranking. */
  async listPublic(signal?: AbortSignal, sort?: 'top-rated'): Promise<CollectionPoint[]> {
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

  /** Um ponto ativo, com os dados públicos do mapa — base da página do ponto (anônimo). */
  async getPublic(id: string, signal?: AbortSignal): Promise<CollectionPoint> {
    const { data } = await http.get<CollectionPoint>(`/public/collection-points/${id}`, { signal })
    return data
  },

  /** Lista administrativa (autenticada) de todos os pontos, ativos e inativos (requer role Admin). */
  async listForManagement(signal?: AbortSignal): Promise<CollectionPoint[]> {
    const { data } = await http.get<CollectionPoint[]>('/collection-points', { signal })
    return data
  },

  /** Consulta um ponto com o estoque (Admin: qualquer ponto; PointManager: apenas o próprio). */
  async getById(id: string, signal?: AbortSignal): Promise<CollectionPoint> {
    const { data } = await http.get<CollectionPoint>(`/collection-points/${id}`, { signal })
    return data
  },

  /** Indicadores de resultado do ponto no período (Admin: qualquer ponto; PointManager: apenas o próprio). */
  async getDashboard(
    id: string,
    period: { from: string; to: string },
    signal?: AbortSignal,
  ): Promise<CollectionPointDashboard> {
    const { data } = await http.get<CollectionPointDashboard>(
      `/collection-points/${id}/dashboard`,
      { signal, params: period },
    )
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

  /** Cadastra um novo item no catálogo de estoque granular do ponto (requer role Admin ou PointManager). */
  async createStockItem(id: string, input: CreateStockItemInput): Promise<CollectionPoint> {
    const { data } = await http.post<CollectionPoint>(`/collection-points/${id}/stock-items`, input)
    return data
  },

  /** Atualiza nome, unidade, categoria e metas de um item de estoque (requer role Admin ou PointManager). */
  async updateStockItem(
    id: string,
    stockItemId: string,
    input: UpdateStockItemInput,
  ): Promise<CollectionPoint> {
    const { data } = await http.put<CollectionPoint>(
      `/collection-points/${id}/stock-items/${stockItemId}`,
      input,
    )
    return data
  },

  /** Remove um item do catálogo de estoque do ponto (requer role Admin ou PointManager). */
  async removeStockItem(id: string, stockItemId: string): Promise<void> {
    await http.delete(`/collection-points/${id}/stock-items/${stockItemId}`)
  },

  /** Salva em bloco as quantidades de vários itens do ponto (aba "Estoque"). */
  async updateStockQuantities(
    id: string,
    input: UpdateStockQuantitiesInput,
  ): Promise<CollectionPoint> {
    const { data } = await http.put<CollectionPoint>(
      `/collection-points/${id}/stock-items/quantities`,
      input,
    )
    return data
  },

  /** Salva em bloco metas, avisos e acompanhamento de vários itens do ponto (aba "Metas e avisos"). */
  async updateStockItemSettings(
    id: string,
    input: UpdateStockItemSettingsInput,
  ): Promise<CollectionPoint> {
    const { data } = await http.put<CollectionPoint>(
      `/collection-points/${id}/stock-items/settings`,
      input,
    )
    return data
  },

  /** Atualiza a regra de retirada do ponto (ciclo + máximo por beneficiário em cada ciclo). */
  async updateWithdrawalRule(id: string, input: UpdateWithdrawalRuleInput): Promise<CollectionPoint> {
    const { data } = await http.put<CollectionPoint>(
      `/collection-points/${id}/withdrawal-rule`,
      input,
    )
    return data
  },

  /** Cria uma nova receita de cesta básica para o ponto (requer role Admin ou PointManager). */
  async createBasketTemplate(
    id: string,
    input: CreateBasketTemplateInput,
  ): Promise<CollectionPoint> {
    const { data } = await http.post<CollectionPoint>(
      `/collection-points/${id}/basket-templates`,
      input,
    )
    return data
  },

  /** Atualiza nome, itens e status de uma receita de cesta básica (requer role Admin ou PointManager). */
  async updateBasketTemplate(
    id: string,
    templateId: string,
    input: UpdateBasketTemplateInput,
  ): Promise<CollectionPoint> {
    const { data } = await http.put<CollectionPoint>(
      `/collection-points/${id}/basket-templates/${templateId}`,
      input,
    )
    return data
  },

  /** Remove uma receita de cesta básica do ponto (requer role Admin ou PointManager). */
  async removeBasketTemplate(id: string, templateId: string): Promise<void> {
    await http.delete(`/collection-points/${id}/basket-templates/${templateId}`)
  },

  /** Cadastra uma nova categoria no catálogo do ponto (requer role Admin ou PointManager). */
  async createCategory(id: string, input: CreateCategoryInput): Promise<CollectionPoint> {
    const { data } = await http.post<CollectionPoint>(`/collection-points/${id}/categories`, input)
    return data
  },

  /** Atualiza nome e ícone de uma categoria existente do ponto (requer role Admin ou PointManager). */
  async updateCategory(
    id: string,
    categoryId: string,
    input: UpdateCategoryInput,
  ): Promise<CollectionPoint> {
    const { data } = await http.put<CollectionPoint>(
      `/collection-points/${id}/categories/${categoryId}`,
      input,
    )
    return data
  },

  /** Remove uma categoria do catálogo do ponto (falha se estiver em uso por item de estoque ou retirada). */
  async removeCategory(id: string, categoryId: string): Promise<void> {
    await http.delete(`/collection-points/${id}/categories/${categoryId}`)
  },
}
