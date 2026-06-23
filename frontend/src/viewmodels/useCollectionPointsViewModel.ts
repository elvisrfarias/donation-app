import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { collectionPointService } from '@/models/services'
import type { CollectionPoint } from '@/models/types'
import type { CreateCollectionPointForm } from '@/viewmodels/schemas'

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

  const mutation = useMutation<CollectionPoint, ApiError, CreateCollectionPointForm>({
    // Envia a imagem primeiro (obrigatória) e cria o ponto referenciando o arquivo.
    mutationFn: async ({ image, ...fields }) => {
      const uploaded = await collectionPointService.uploadImage(image)
      return collectionPointService.create({ ...fields, imageFileId: uploaded.id })
    },
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
