import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { collectionPointService } from '@/models/services/collectionPointService'
import type { CollectionPoint } from '@/models/types'

/** Ponto público para a página própria do ponto; 404 vira `isNotFound` (sem nova tentativa). */
export function usePublicCollectionPoint(pointId: string) {
  const query = useQuery<CollectionPoint, ApiError>({
    queryKey: queryKeys.collectionPoints.publicDetail(pointId),
    queryFn: ({ signal }) => collectionPointService.getPublic(pointId, signal),
    retry: (failureCount, error) => !error.isNotFound && failureCount < 1,
  })

  return {
    point: query.data ?? null,
    isLoading: query.isLoading,
    isNotFound: query.error?.isNotFound ?? false,
    isError: query.isError,
  }
}
