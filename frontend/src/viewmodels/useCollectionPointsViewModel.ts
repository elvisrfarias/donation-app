import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import { ApiError } from '@/models/api/problemDetails'
import { cepService, collectionPointService, geocodingService } from '@/models/services'
import { formatAddress, type Address, type CollectionPoint, type LatLng } from '@/models/types'
import type { CreateCollectionPointForm, UpdateCollectionPointForm } from '@/viewmodels/schemas'

type AddressWithCoordinates = Address & { latitude?: number | null; longitude?: number | null }

/**
 * Garante as coordenadas exatas do ponto antes de salvar (fluxo bloqueante):
 * 1) usa o local escolhido no mapa, quando houver; senão
 * 2) geocodifica o endereço no Nominatim; senão
 * 3) tenta as coordenadas do CEP na BrasilAPI; senão
 * falha pedindo para marcar o local no mapa — nunca grava um ponto sem posição.
 */
async function ensureCoordinates(fields: AddressWithCoordinates): Promise<LatLng> {
  if (fields.latitude != null && fields.longitude != null) {
    return { lat: fields.latitude, lng: fields.longitude }
  }

  const geocoded = await geocodingService.geocode(formatAddress(fields))
  if (geocoded) return geocoded

  const byCep = await cepService.lookup(fields.zipCode)
  if (byCep?.position) return byCep.position

  throw new ApiError({
    status: 0,
    title: 'Endereço não localizado',
    detail:
      'Não foi possível localizar este endereço no mapa. Revise rua, número, cidade, UF e CEP, ou use "Selecionar no mapa" para marcar o local exato.',
  })
}

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
    mutationFn: async ({ image, ...fields }) => {
      const { lat, lng } = await ensureCoordinates(fields)
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

/** Mutação de atualização de ponto de coleta (Admin). */
export function useUpdateCollectionPoint() {
  const queryClient = useQueryClient()

  const mutation = useMutation<
    CollectionPoint,
    ApiError,
    { id: string; values: UpdateCollectionPointForm }
  >({
    mutationFn: async ({ id, values: { image, ...fields } }) => {
      const { lat, lng } = await ensureCoordinates(fields)
      const imageFileId = image ? (await collectionPointService.uploadImage(image)).id : undefined
      return collectionPointService.update(id, { ...fields, imageFileId })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.collectionPoints.public })
    },
  })

  return {
    update: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    updatedPoint: mutation.data,
    reset: mutation.reset,
  }
}
