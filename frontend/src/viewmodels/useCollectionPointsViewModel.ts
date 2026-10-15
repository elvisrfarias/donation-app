import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query'
import { useApiList, useApiMutation } from '@/lib/apiHooks'
import type { DashboardPeriod } from '@/lib/dashboardPeriod'
import { queryKeys } from '@/lib/queryClient'
import { ApiError } from '@/models/api/problemDetails'
import { collectionPointService } from '@/models/services'
import {
  type Address,
  type CollectionPoint,
  type CollectionPointDashboard,
  type CreateBasketTemplateInput,
  type CreateCategoryInput,
  type CreateStockItemInput,
  type LatLng,
  type UpdateBasketTemplateInput,
  type UpdateCategoryInput,
  type UpdateStockItemInput,
  type UpdateStockItemSettingsInput,
  type UpdateStockQuantitiesInput,
  type UpdateWithdrawalRuleInput,
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
 * Exige o local exato marcado no mapa antes de salvar. Não há mais geocodificação automática nem
 * fallback por CEP: essas posições eram aproximadas e eram gravadas como se fossem exatas.
 */
function requireExactCoordinates(fields: AddressWithCoordinates): LatLng {
  if (fields.latitude == null || fields.longitude == null) {
    throw new ApiError({
      status: 0,
      title: 'Local exato não marcado',
      detail: 'Marque o local exato do ponto no mapa ("Marcar no mapa") para poder salvar.',
    })
  }
  return { lat: fields.latitude, lng: fields.longitude }
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
      const { lat, lng } = requireExactCoordinates(fields)
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
      const { lat, lng } = requireExactCoordinates(fields)
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

/** Mutação de criação de um item no catálogo de estoque granular do ponto (Admin ou PointManager). */
export function useCreateStockItem() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    CollectionPoint,
    { pointId: string; input: CreateStockItemInput }
  >({
    mutationFn: ({ pointId, input }) => collectionPointService.createStockItem(pointId, input),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
    },
  })

  return { createStockItem: mutate, ...rest }
}

/** Mutação de atualização de nome, unidade, categoria e metas de um item de estoque (não a quantidade). */
export function useUpdateStockItem() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    CollectionPoint,
    { pointId: string; stockItemId: string; input: UpdateStockItemInput }
  >({
    mutationFn: ({ pointId, stockItemId, input }) =>
      collectionPointService.updateStockItem(pointId, stockItemId, input),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
    },
  })

  return { updateStockItem: mutate, ...rest }
}

/** Mutação de remoção de um item de estoque (falha se ele estiver em uso em alguma receita de cesta). */
export function useRemoveStockItem() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<void, { pointId: string; stockItemId: string }>({
    mutationFn: ({ pointId, stockItemId }) =>
      collectionPointService.removeStockItem(pointId, stockItemId),
    onSuccess: (_, { pointId }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.collectionPoints.detail(pointId) })
    },
  })

  return { removeStockItem: mutate, ...rest }
}

/** Mutação de salvamento em bloco das quantidades de vários itens do ponto (aba "Estoque"). */
export function useUpdateStockQuantities() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    CollectionPoint,
    { pointId: string; input: UpdateStockQuantitiesInput }
  >({
    mutationFn: ({ pointId, input }) =>
      collectionPointService.updateStockQuantities(pointId, input),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
    },
  })

  return { updateStockQuantities: mutate, ...rest }
}

/** Mutação de salvamento em bloco de metas, avisos e acompanhamento de vários itens (aba "Metas e avisos"). */
export function useUpdateStockItemSettings() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    CollectionPoint,
    { pointId: string; input: UpdateStockItemSettingsInput }
  >({
    mutationFn: ({ pointId, input }) =>
      collectionPointService.updateStockItemSettings(pointId, input),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
    },
  })

  return { updateStockItemSettings: mutate, ...rest }
}

/** Mutação de atualização da regra de retirada do ponto (ciclo + máximo por ciclo), à parte das metas. */
export function useUpdateWithdrawalRule() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    CollectionPoint,
    { pointId: string; input: UpdateWithdrawalRuleInput }
  >({
    mutationFn: ({ pointId, input }) => collectionPointService.updateWithdrawalRule(pointId, input),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
    },
  })

  return { updateWithdrawalRule: mutate, ...rest }
}

/** Mutação de criação de uma receita de cesta básica para o ponto (ativa por padrão). */
export function useCreateBasketTemplate() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    CollectionPoint,
    { pointId: string; input: CreateBasketTemplateInput }
  >({
    mutationFn: ({ pointId, input }) => collectionPointService.createBasketTemplate(pointId, input),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
    },
  })

  return { createBasketTemplate: mutate, ...rest }
}

/** Mutação de atualização de nome, itens e status (ativa/inativa) de uma receita de cesta básica. */
export function useUpdateBasketTemplate() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    CollectionPoint,
    { pointId: string; templateId: string; input: UpdateBasketTemplateInput }
  >({
    mutationFn: ({ pointId, templateId, input }) =>
      collectionPointService.updateBasketTemplate(pointId, templateId, input),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
    },
  })

  return { updateBasketTemplate: mutate, ...rest }
}

/** Mutação de remoção de uma receita de cesta básica (falha se ela já foi usada em alguma retirada). */
export function useRemoveBasketTemplate() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<void, { pointId: string; templateId: string }>({
    mutationFn: ({ pointId, templateId }) =>
      collectionPointService.removeBasketTemplate(pointId, templateId),
    onSuccess: (_, { pointId }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.collectionPoints.detail(pointId) })
    },
  })

  return { removeBasketTemplate: mutate, ...rest }
}

/** Mutação de criação de uma categoria no catálogo do ponto (Admin ou PointManager). */
export function useCreateCategory() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    CollectionPoint,
    { pointId: string; input: CreateCategoryInput }
  >({
    mutationFn: ({ pointId, input }) => collectionPointService.createCategory(pointId, input),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
    },
  })

  return { createCategory: mutate, ...rest }
}

/** Mutação de atualização de nome e ícone de uma categoria do ponto. */
export function useUpdateCategory() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    CollectionPoint,
    { pointId: string; categoryId: string; input: UpdateCategoryInput }
  >({
    mutationFn: ({ pointId, categoryId, input }) =>
      collectionPointService.updateCategory(pointId, categoryId, input),
    onSuccess: (updatedPoint) => {
      queryClient.setQueryData(queryKeys.collectionPoints.detail(updatedPoint.id), updatedPoint)
    },
  })

  return { updateCategory: mutate, ...rest }
}

/** Mutação de remoção de uma categoria do ponto (falha se estiver em uso por item de estoque ou retirada). */
export function useRemoveCategory() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<void, { pointId: string; categoryId: string }>({
    mutationFn: ({ pointId, categoryId }) =>
      collectionPointService.removeCategory(pointId, categoryId),
    onSuccess: (_, { pointId }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.collectionPoints.detail(pointId) })
    },
  })

  return { removeCategory: mutate, ...rest }
}
