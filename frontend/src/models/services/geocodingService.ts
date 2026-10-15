import { env } from '@/lib/env'
import { logger } from '@/lib/logger'
import { addressSearchSteps } from '@/lib/addressSearchSteps'
import type { Address, GeocodedAddress, LatLng } from '@/models/types'

const CACHE_KEY = 'doacao.geocode.cache.v1'
const MIN_INTERVAL_MS = 1100

type CacheMap = Record<string, LatLng | null>

function loadCache(): CacheMap {
  try {
    const raw = localStorage.getItem(CACHE_KEY)
    return raw ? (JSON.parse(raw) as CacheMap) : {}
  } catch {
    return {}
  }
}

function saveCache(cache: CacheMap): void {
  try {
    localStorage.setItem(CACHE_KEY, JSON.stringify(cache))
  } catch {}
}

const cache = loadCache()
let lastRequestAt = 0

interface GeocodingJob {
  run: () => Promise<unknown>
  resolve: (value: unknown) => void
  urgent: boolean
  isAlreadyCached: () => boolean
}

const pendingJobs: GeocodingJob[] = []
let draining = false

function takeNextJob(): GeocodingJob {
  const urgentIndex = pendingJobs.findIndex((job) => job.urgent)
  return pendingJobs.splice(urgentIndex >= 0 ? urgentIndex : 0, 1)[0]
}

async function waitForRateLimit(): Promise<void> {
  const wait = Math.max(0, MIN_INTERVAL_MS - (Date.now() - lastRequestAt))
  if (wait > 0) await new Promise((resolve) => setTimeout(resolve, wait))
  lastRequestAt = Date.now()
}

async function drainJobs(): Promise<void> {
  if (draining) return
  draining = true
  while (pendingJobs.length > 0) {
    const job = takeNextJob()
    if (!job.isAlreadyCached()) await waitForRateLimit()
    job.resolve(await job.run())
  }
  draining = false
}

function schedule<T>(
  run: () => Promise<T>,
  options: { urgent?: boolean; isAlreadyCached?: () => boolean } = {},
): Promise<T> {
  return new Promise<T>((resolve) => {
    pendingJobs.push({
      run,
      resolve: resolve as (value: unknown) => void,
      urgent: options.urgent ?? false,
      isAlreadyCached: options.isAlreadyCached ?? (() => false),
    })
    void drainJobs()
  })
}

/** Opções de uma geocodificação. `urgent` fura a fila (ex.: o usuário clicou no ponto na lista). */
export interface GeocodeOptions {
  signal?: AbortSignal
  urgent?: boolean
}

function normalizeKey(q: string): string {
  return q.trim().toLowerCase().replace(/\s+/g, ' ')
}

/** Endereço resolvido a partir de coordenadas (reverse geocoding). */
export interface ReverseAddress {
  street: string
  number: string
  district: string
  city: string
  state: string
  zipCode: string
}

interface NominatimAddress {
  road?: string
  pedestrian?: string
  footway?: string
  house_number?: string
  suburb?: string
  neighbourhood?: string
  city_district?: string
  city?: string
  town?: string
  village?: string
  municipality?: string
  state?: string
  postcode?: string
  'ISO3166-2-lvl4'?: string
}

async function requestNominatim(query: string, signal?: AbortSignal): Promise<LatLng | null> {
  const url = new URL(`${env.nominatimUrl}/search`)
  url.searchParams.set('q', query)
  url.searchParams.set('format', 'jsonv2')
  url.searchParams.set('limit', '1')
  url.searchParams.set('countrycodes', 'br')

  const res = await fetch(url, { signal, headers: { Accept: 'application/json' } })
  if (!res.ok) throw new Error(`Nominatim respondeu ${res.status}`)

  const results = (await res.json()) as Array<{ lat: string; lon: string }>
  const first = results[0]
  if (!first) return null
  return { lat: Number(first.lat), lng: Number(first.lon) }
}

function parseReverse(address: NominatimAddress): ReverseAddress {
  const uf = address['ISO3166-2-lvl4']?.split('-')[1] ?? ''
  return {
    street: address.road ?? address.pedestrian ?? address.footway ?? '',
    number: address.house_number ?? '',
    district: address.suburb ?? address.neighbourhood ?? address.city_district ?? '',
    city: address.city ?? address.town ?? address.village ?? address.municipality ?? '',
    state: uf.toUpperCase(),
    zipCode: (address.postcode ?? '').replace(/\D/g, ''),
  }
}

async function requestNominatimReverse(
  position: LatLng,
  signal?: AbortSignal,
): Promise<ReverseAddress | null> {
  const url = new URL(`${env.nominatimUrl}/reverse`)
  url.searchParams.set('lat', String(position.lat))
  url.searchParams.set('lon', String(position.lng))
  url.searchParams.set('format', 'jsonv2')
  url.searchParams.set('addressdetails', '1')

  const res = await fetch(url, { signal, headers: { Accept: 'application/json' } })
  if (!res.ok) throw new Error(`Nominatim respondeu ${res.status}`)

  const data = (await res.json()) as { address?: NominatimAddress }
  if (!data.address) return null
  return parseReverse(data.address)
}

export const geocodingService = {
  /**
   * Converte um endereço em coordenadas usando o Nominatim (OpenStreetMap).
   * Resultados são cacheados em localStorage e as chamadas são serializadas
   * respeitando o limite de 1 req/s da API pública.
   */
  async geocode(query: string, options: GeocodeOptions = {}): Promise<LatLng | null> {
    const key = normalizeKey(query)
    if (key in cache) return cache[key]

    return schedule(
      async () => {
        if (key in cache) return cache[key]
        try {
          const position = await requestNominatim(query, options.signal)
          cache[key] = position
          saveCache(cache)
          return position
        } catch {
          return null
        }
      },
      { urgent: options.urgent, isAlreadyCached: () => key in cache },
    )
  },

  /**
   * Localiza um endereço tentando consultas cada vez menos precisas (ver `addressSearchSteps`).
   * Devolve a primeira posição encontrada e se ela é só a da cidade.
   */
  async geocodeAddress(
    address: Address,
    options: GeocodeOptions = {},
  ): Promise<GeocodedAddress | null> {
    for (const step of addressSearchSteps(address)) {
      const position = await geocodingService.geocode(step.query, options)
      if (position) return { position, approximate: step.approximate }
    }
    return null
  },

  /**
   * Resolve o endereço de um par de coordenadas (reverse geocoding). Usado pelo
   * seletor de mapa para preencher os campos a partir do ponto escolhido.
   */
  async reverse(position: LatLng, signal?: AbortSignal): Promise<ReverseAddress | null> {
    return schedule(
      async () => {
        try {
          return await requestNominatimReverse(position, signal)
        } catch (error) {
          logger.warn('Falha no reverse geocoding; seguindo sem endereço.', { position, error })
          return null
        }
      },
      { urgent: true },
    )
  },
}
