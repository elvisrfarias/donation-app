import { http } from '@/models/api/httpClient'
import type { Notice, PublishNoticeInput } from '@/models/types'

export const noticeService = {
  /** Avisos públicos do mural de um ponto (anônimo). */
  async listPublic(pointId: string, signal?: AbortSignal): Promise<Notice[]> {
    const { data } = await http.get<Notice[]>(`/public/collection-points/${pointId}/notices`, {
      signal,
    })
    return data
  },

  /** Mural da rede: avisos ativos de todos os pontos (gestão). */
  async listNetwork(signal?: AbortSignal): Promise<Notice[]> {
    const { data } = await http.get<Notice[]>('/notices', { signal })
    return data
  },

  /** Publica um aviso no mural do ponto (gestão). */
  async publish(pointId: string, input: PublishNoticeInput): Promise<Notice> {
    const { data } = await http.post<Notice>(`/collection-points/${pointId}/notices`, input)
    return data
  },

  /** Retira um aviso do mural (gestão). */
  async archive(pointId: string, noticeId: string): Promise<void> {
    await http.delete(`/collection-points/${pointId}/notices/${noticeId}`)
  },
}
