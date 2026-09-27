import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { useApiList, useApiMutation } from '@/lib/apiHooks'
import type { DashboardPeriod } from '@/lib/dashboardPeriod'
import { queryKeys } from '@/lib/queryClient'
import { ApiError } from '@/models/api/problemDetails'
import { cepService, collectionPointService, geocodingService } from '@/models/services'
import {
  formatAddress,
  type Address,
  type CollectionPoint,
  type CollectionPointDashboard,
  type LatLng,
  type StockItem,
  type UpdateStockSettingsInput,
} from '@/models/types'
import type { CreateCollectionPointForm, UpdateCollectionPointForm } from '@/viewmodels/schemas'

type AddressWithCoordinates = Address & { latitude?: number | null; longitude?: number | null }

function invalidateCollectionPointLists(queryClient: ReturnType<typeof useQueryClient>) {
  queryClient.invalidateQueries({ queryKey: queryKeys.collectionPoints.public })
  queryClient.invalidateQueries({ queryKey: queryKeys.collectionPoints.management })
}

/** Converte o textarea de necessidades (uma por linha) na lista enviada à API. */
function parseNeeds(needs: string | undefined): string[] {
  return (needs ?? '')
    .split('\n')
    .map((n) => n.trim())
    .filter(Boolean)
}

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
  const { items, ...rest } = useApiList<CollectionPoint>({
    queryKey: queryKeys.collectionPoints.public,
    queryFn: ({ signal }) => collectionPointService.listPublic(signal),
  })

  return { points: items, ...rest }
}

/** Lista administrativa (todos os pontos, ativos e inativos) — só para telas Admin-only. */
export function useCollectionPointsForManagement(options?: { enabled?: boolean }) {
  const { items, ...rest } = useApiList<CollectionPoint>({
    queryKey: queryKeys.collectionPoints.management,
    queryFn: ({ signal }) => collectionPointService.listForManagement(signal),
    enabled: options?.enabled ?? true,
  })

  return { points: items, ...rest }
}

/** Um único ponto (com estoque). O backend limita o gestor ao próprio ponto. */
export function useCollectionPoint(pointId: string | null) {
  const query = useQuery<CollectionPoint, ApiError>({
    queryKey: queryKeys.collectionPoints.detail(pointId ?? ''),
    queryFn: ({ signal }) => collectionPointService.getById(pointId ?? '', signal),
    enabled: pointId !== null,
  })

  return {
    point: query.data ?? null,
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
    refetch: query.refetch,
  }
}

/** Indicadores de resultado de um único ponto (retiradas por mês, atendimentos, últimas retiradas). */
export function useCollectionPointDashboard(
  pointId: string | null,
  period: DashboardPeriod,
  enabled = true,
) {
  const query = useQuery<CollectionPointDashboard, ApiError>({
    queryKey: queryKeys.collectionPoints.dashboard(pointId ?? '', period.from, period.to),
    queryFn: ({ signal }) => collectionPointService.getDashboard(pointId ?? '', period, signal),
    enabled: pointId !== null && enabled,
    placeholderData: keepPreviousData,
  })

  return {
    dashboard: query.data ?? null,
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
  }
}

/** Mutação de criação de ponto de coleta (Admin). */
export function useCreateCollectionPoint() {
  const queryClient = useQueryClient()

  const { mutate, data, ...rest } = useApiMutation<CollectionPoint, CreateCollectionPointForm>({
    mutationFn: async ({ image, needs, ...fields }) => {
      const { lat, lng } = await ensureCoordinates(fields)
      const uploaded = await collectionPointService.uploadImage(image)
      return collectionPointService.create({
        ...fields,
        needs: parseNeeds(needs),
        imageFileId: uploaded.id,
        latitude: lat,
        longitude: lng,
      })
    },
    onSuccess: () => invalidateCollectionPointLists(queryClient),
  })

  return { create: mutate, createdPoint: data, ...rest }
}

/** Mutação de atualização de ponto de coleta (Admin). */
export function useUpdateCollectionPoint() {
  const queryClient = useQueryClient()

  const { mutate, data, ...rest } = useApiMutation<
    CollectionPoint,
    { id: string; values: UpdateCollectionPointForm }
  >({
    mutationFn: async ({ id, values: { image, needs, ...fields } }) => {
      const { lat, lng } = await ensureCoordinates(fields)
      const imageFileId = image ? (await collectionPointService.uploadImage(image)).id : undefined
      return collectionPointService.update(id, {
        ...fields,
        needs: parseNeeds(needs),
        imageFileId,
        latitude: lat,
        longitude: lng,
      })
    },
    onSuccess: () => invalidateCollectionPointLists(queryClient),
  })

  return { update: mutate, updatedPoint: data, ...rest }
}

/** Mutação de atualização do estoque de um ponto (Admin ou PointManager). */
export function useUpdateStock() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<CollectionPoint, { id: string; items: StockItem[] }>({
    mutationFn: ({ id, items }) => collectionPointService.updateStock(id, items),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
      invalidateCollectionPointLists(queryClient)
    },
  })

  return { updateStock: mutate, ...rest }
}

/** Mutação das metas, avisos, categorias acompanhadas e regra de retirada do ponto (Admin ou PointManager). */
export function useUpdateStockSettings() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    CollectionPoint,
    { id: string; input: UpdateStockSettingsInput }
  >({
    mutationFn: ({ id, input }) => collectionPointService.updateStockSettings(id, input),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
    },
  })

  return { updateStockSettings: mutate, ...rest }
}
