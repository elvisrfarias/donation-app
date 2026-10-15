import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { activityService } from '@/models/services'
import type { ActivityFeed } from '@/models/types'

const ACTIVITY_REFRESH_INTERVAL_MS = 60_000

/** Feed de percepção do ponto: atualiza sozinho a cada minuto enquanto a tela está aberta. */
export function useActivityFeed(pointId: string | null) {
  const query = useQuery<ActivityFeed, ApiError>({
    queryKey: queryKeys.activity.byPoint(pointId ?? ''),
    queryFn: ({ signal }) => activityService.getByPoint(pointId as string, signal),
    enabled: pointId !== null,
    refetchInterval: ACTIVITY_REFRESH_INTERVAL_MS,
  })

  return {
    items: query.data?.items ?? [],
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
  }
}
