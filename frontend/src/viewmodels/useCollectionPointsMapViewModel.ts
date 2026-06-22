import { useMemo, useState } from 'react'
import { useQueries, useQuery } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { collectionPointService, geocodingService } from '@/models/services'
import {
  formatAddress,
  type CollectionPoint,
  type CollectionPointType,
  type Geocoded,
} from '@/models/types'

export interface MapFilters {
  search: string
  type: CollectionPointType | 'all'
  city: string | 'all'
}

const EMPTY_FILTERS: MapFilters = { search: '', type: 'all', city: 'all' }

/**
 * ViewModel do mapa público. Busca os pontos, geocodifica cada endereço via
 * OpenStreetMap (Nominatim) e expõe a lista filtrável + a seleção atual.
 */
export function useCollectionPointsMapViewModel() {
  const pointsQuery = useQuery<CollectionPoint[], ApiError>({
    queryKey: queryKeys.collectionPoints.public,
    queryFn: ({ signal }) => collectionPointService.listPublic(signal),
  })

  const points = useMemo(() => pointsQuery.data ?? [], [pointsQuery.data])

  // Uma query de geocodificação por ponto (cacheada "para sempre" — endereços não mudam).
  const geocodeQueries = useQueries({
    queries: points.map((p) => {
      const query = formatAddress(p.address)
      return {
        queryKey: queryKeys.geocode(query),
        queryFn: () => geocodingService.geocode(query),
        staleTime: Infinity,
        gcTime: Infinity,
      }
    }),
  })

  const geocoded: Geocoded<CollectionPoint>[] = useMemo(
    () => points.map((item, i) => ({ item, position: geocodeQueries[i]?.data ?? null })),
    [points, geocodeQueries],
  )

  const [filters, setFilters] = useState<MapFilters>(EMPTY_FILTERS)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const cities = useMemo(
    () => Array.from(new Set(points.map((p) => p.address.city))).sort((a, b) => a.localeCompare(b)),
    [points],
  )

  const filtered = useMemo(() => {
    const term = filters.search.trim().toLowerCase()
    return geocoded.filter(({ item }) => {
      if (filters.type !== 'all' && item.type !== filters.type) return false
      if (filters.city !== 'all' && item.address.city !== filters.city) return false
      if (term) {
        const haystack =
          `${item.name} ${item.address.city} ${item.address.district}`.toLowerCase()
        if (!haystack.includes(term)) return false
      }
      return true
    })
  }, [geocoded, filters])

  const located = useMemo(() => geocoded.filter((g) => g.position).length, [geocoded])
  const geocodingPending = geocodeQueries.some((q) => q.isPending || q.isFetching)

  return {
    isLoading: pointsQuery.isLoading,
    isError: pointsQuery.isError,
    error: pointsQuery.error,
    refetch: pointsQuery.refetch,
    geocoded,
    filtered,
    cities,
    filters,
    setFilters,
    resetFilters: () => setFilters(EMPTY_FILTERS),
    selectedId,
    setSelectedId,
    total: points.length,
    located,
    geocodingPending,
  }
}
