import { http } from '@/models/api/httpClient'
import type { ActivityFeed } from '@/models/types'

export const activityService = {
  /** Feed de atividade recente do ponto (gestão). */
  async getByPoint(pointId: string, signal?: AbortSignal): Promise<ActivityFeed> {
    const { data } = await http.get<ActivityFeed>(`/collection-points/${pointId}/activity`, {
      signal,
    })
    return data
  },
}
