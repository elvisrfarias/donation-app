import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { collectionPointService } from '@/models/services'
import type { CollectionPoint, CreateCollectionPointInput } from '@/models/types'

/** Lista de pontos de coleta (reaproveita o endpoint público). */
export function useCollectionPoints() {
  const query = useQuery<CollectionPoint[], ApiError>({
    queryKey: queryKeys.collectionPoints.public,
    queryFn: ({ signal }) => collectionPointService.listPublic(signal),
  })

  return {
    points: query.data ?? [],
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
    refetch: query.refetch,
  }
}

/** Mutação de criação de ponto de coleta (Admin). */
export function useCreateCollectionPoint() {
  const queryClient = useQueryClient()

  const mutation = useMutation<CollectionPoint, ApiError, CreateCollectionPointInput>({
    mutationFn: (input) => collectionPointService.create(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.collectionPoints.public })
    },
  })

  return {
    create: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    createdPoint: mutation.data,
    reset: mutation.reset,
  }
}
