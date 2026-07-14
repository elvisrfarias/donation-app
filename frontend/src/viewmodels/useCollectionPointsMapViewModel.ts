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
  type LatLng,
} from '@/models/types'

export interface MapFilters {
  search: string
  type: CollectionPointType | 'all'
  city: string | 'all'
}

export type LocatedCollectionPoint = Geocoded<CollectionPoint> & { distanceKm: number | null }

const EMPTY_FILTERS: MapFilters = { search: '', type: 'all', city: 'all' }

function storedPosition(point: CollectionPoint) {
  const { latitude, longitude } = point.address
  return latitude != null && longitude != null ? { lat: latitude, lng: longitude } : null
}

/** Distância aproximada em km entre dois pontos (fórmula de Haversine). */
function haversineKm(a: LatLng, b: LatLng): number {
  const toRad = (deg: number) => (deg * Math.PI) / 180
  const earthRadiusKm = 6371
  const dLat = toRad(b.lat - a.lat)
  const dLng = toRad(b.lng - a.lng)
  const lat1 = toRad(a.lat)
  const lat2 = toRad(b.lat)
  const h =
    Math.sin(dLat / 2) ** 2 + Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLng / 2) ** 2
  return 2 * earthRadiusKm * Math.asin(Math.sqrt(h))
}

/**
 * ViewModel do mapa público. Busca os pontos e usa as coordenadas persistidas do ponto
 * quando existem; só recorre à geocodificação (OpenStreetMap/Nominatim) para os pontos
 * antigos sem coordenadas. Expõe a lista filtrável, a ordenação (ranking) e a seleção.
 */
export function useCollectionPointsMapViewModel() {
  const [sortByRating, setSortByRating] = useState(false)

  const pointsQuery = useQuery<CollectionPoint[], ApiError>({
    queryKey: sortByRating
      ? [...queryKeys.collectionPoints.public, 'top-rated']
      : queryKeys.collectionPoints.public,
    queryFn: ({ signal }) =>
      collectionPointService.listPublic(signal, sortByRating ? 'top-rated' : undefined),
  })

  const points = useMemo(() => pointsQuery.data ?? [], [pointsQuery.data])

  const geocodeQueries = useQueries({
    queries: points.map((p) => {
      const query = formatAddress(p.address)
      const hasStored = storedPosition(p) !== null
      return {
        queryKey: queryKeys.geocode(query),
        queryFn: () => geocodingService.geocode(query),
        staleTime: Infinity,
        gcTime: Infinity,
        enabled: !hasStored,
      }
    }),
  })

  const geocoded: Geocoded<CollectionPoint>[] = useMemo(
    () =>
      points.map((item, i) => ({
        item,
        position: storedPosition(item) ?? geocodeQueries[i]?.data ?? null,
      })),
    [points, geocodeQueries],
  )

  const [filters, setFilters] = useState<MapFilters>(EMPTY_FILTERS)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [userLocation, setUserLocation] = useState<LatLng | null>(null)
  const [locating, setLocating] = useState(false)

  function locate() {
    if (typeof navigator === 'undefined' || !navigator.geolocation) return
    setLocating(true)
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setUserLocation({ lat: pos.coords.latitude, lng: pos.coords.longitude })
        setLocating(false)
      },
      () => setLocating(false),
      { enableHighAccuracy: true, timeout: 10_000 },
    )
  }

  const cities = useMemo(
    () => Array.from(new Set(points.map((p) => p.address.city))).sort((a, b) => a.localeCompare(b)),
    [points],
  )

  const filtered = useMemo<LocatedCollectionPoint[]>(() => {
    const term = filters.search.trim().toLowerCase()
    const matches = geocoded
      .filter(({ item }) => {
        if (filters.type !== 'all' && item.type !== filters.type) return false
        if (filters.city !== 'all' && item.address.city !== filters.city) return false
        if (term) {
          const haystack =
            `${item.name} ${item.address.city} ${item.address.district}`.toLowerCase()
          if (!haystack.includes(term)) return false
        }
        return true
      })
      .map((g) => ({
        ...g,
        distanceKm: userLocation && g.position ? haversineKm(userLocation, g.position) : null,
      }))

    if (userLocation) {
      matches.sort((a, b) => {
        if (a.distanceKm == null) return 1
        if (b.distanceKm == null) return -1
        return a.distanceKm - b.distanceKm
      })
    }
    return matches
  }, [geocoded, filters, userLocation])

  const located = useMemo(() => geocoded.filter((g) => g.position).length, [geocoded])
  const geocodingPending = geocodeQueries.some((q) => q.isFetching)

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
    sortByRating,
    setSortByRating,
    userLocation,
    locate,
    locating,
    canLocate: typeof navigator !== 'undefined' && !!navigator.geolocation,
  }
}
