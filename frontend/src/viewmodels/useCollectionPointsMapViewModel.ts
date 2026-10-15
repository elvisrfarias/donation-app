import { useEffect, useMemo, useRef, useState } from 'react'
import { useQueries, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { collectionPointService, geocodingService } from '@/models/services'
import {
  formatAddress,
  type CollectionPoint,
  type CollectionPointType,
  type Geocoded,
  type GeocodedAddress,
  type LatLng,
} from '@/models/types'

export interface MapFilters {
  search: string
  type: CollectionPointType | 'all'
  city: string | 'all'
}

/** Pedido para levar a câmera do mapa até um ponto. O `nonce` muda a cada clique, para que
 *  clicar de novo no mesmo ponto também refaça o voo e reabra o popup. */
export interface FocusRequest {
  id: string
  nonce: number
}

export type LocatedCollectionPoint = Geocoded<CollectionPoint> & { distanceKm: number | null }

const EMPTY_FILTERS: MapFilters = { search: '', type: 'all', city: 'all' }

const PRECISE_ACCURACY_METERS = 100
const APPROXIMATE_ACCURACY_METERS = 1_000
const LOCATE_TIMEOUT_MS = 15_000

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
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLng / 2) ** 2
  return 2 * earthRadiusKm * Math.asin(Math.sqrt(h))
}

/**
 * ViewModel do mapa público. Busca os pontos e usa as coordenadas persistidas do ponto
 * quando existem; só recorre à geocodificação (OpenStreetMap/Nominatim) para os pontos
 * antigos sem coordenadas. Expõe a lista filtrável, a ordenação (ranking) e a seleção.
 */
export function useCollectionPointsMapViewModel() {
  const queryClient = useQueryClient()
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
        queryKey: queryKeys.geocodeAddress(query),
        queryFn: () => geocodingService.geocodeAddress(p.address),
        staleTime: Infinity,
        gcTime: Infinity,
        enabled: !hasStored,
      }
    }),
  })

  const geocoded: Geocoded<CollectionPoint>[] = useMemo(
    () =>
      points.map((item, i) => {
        const stored = storedPosition(item)
        const resolved = geocodeQueries[i]?.data
        return {
          item,
          position: stored ?? resolved?.position ?? null,
          approximate: stored === null && (resolved?.approximate ?? false),
        }
      }),
    [points, geocodeQueries],
  )

  const [filters, setFilters] = useState<MapFilters>(EMPTY_FILTERS)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [focusRequest, setFocusRequest] = useState<FocusRequest | null>(null)
  const [unlocatablePointName, setUnlocatablePointName] = useState<string | null>(null)
  const focusCounter = useRef(0)
  const [userLocation, setUserLocation] = useState<LatLng | null>(null)
  const [locationAccuracyMeters, setLocationAccuracyMeters] = useState<number | null>(null)
  const [locating, setLocating] = useState(false)
  const watchId = useRef<number | null>(null)
  const stopTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  function stopLocating() {
    if (watchId.current !== null) navigator.geolocation.clearWatch(watchId.current)
    if (stopTimer.current !== null) clearTimeout(stopTimer.current)
    watchId.current = null
    stopTimer.current = null
    setLocating(false)
  }

  useEffect(
    () => () => {
      if (watchId.current !== null) navigator.geolocation.clearWatch(watchId.current)
      if (stopTimer.current !== null) clearTimeout(stopTimer.current)
    },
    [],
  )

  function locate() {
    if (typeof navigator === 'undefined' || !navigator.geolocation) return
    stopLocating()
    setLocating(true)
    let bestAccuracy = Infinity
    watchId.current = navigator.geolocation.watchPosition(
      ({ coords }) => {
        if (coords.accuracy < bestAccuracy) {
          bestAccuracy = coords.accuracy
          setUserLocation({ lat: coords.latitude, lng: coords.longitude })
          setLocationAccuracyMeters(coords.accuracy)
        }
        if (coords.accuracy <= PRECISE_ACCURACY_METERS) stopLocating()
      },
      stopLocating,
      { enableHighAccuracy: true, maximumAge: 0, timeout: LOCATE_TIMEOUT_MS },
    )
    stopTimer.current = setTimeout(stopLocating, LOCATE_TIMEOUT_MS)
  }

  async function resolvePosition(point: CollectionPoint): Promise<LatLng | null> {
    const known = geocoded.find((entry) => entry.item.id === point.id)?.position
    if (known) return known

    const found = await geocodingService.geocodeAddress(point.address, { urgent: true })
    queryClient.setQueryData<GeocodedAddress | null>(
      queryKeys.geocodeAddress(formatAddress(point.address)),
      found,
    )
    return found?.position ?? null
  }

  async function focusPoint(id: string) {
    const point = points.find((candidate) => candidate.id === id)
    setSelectedId(id)
    setUnlocatablePointName(null)
    if (!point) return

    const position = await resolvePosition(point)
    if (!position) {
      setUnlocatablePointName(point.name)
      return
    }
    focusCounter.current += 1
    setFocusRequest({ id, nonce: focusCounter.current })
  }

  function pickLocation(position: LatLng) {
    stopLocating()
    setUserLocation(position)
    setLocationAccuracyMeters(null)
  }

  const locationIsApproximate =
    locationAccuracyMeters !== null && locationAccuracyMeters > APPROXIMATE_ACCURACY_METERS

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
    focusRequest,
    focusPoint,
    unlocatablePointName,
    total: points.length,
    located,
    geocodingPending,
    sortByRating,
    setSortByRating,
    userLocation,
    locationAccuracyMeters,
    locationIsApproximate,
    locate,
    pickLocation,
    locating,
    canLocate: typeof navigator !== 'undefined' && !!navigator.geolocation,
  }
}
