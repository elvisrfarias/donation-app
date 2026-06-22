import { env } from '@/lib/env'
import type { LatLng } from '@/models/types'

const CACHE_KEY = 'doacao.geocode.cache.v1'
// Política de uso do Nominatim: no máximo 1 requisição por segundo.
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
  } catch {
    /* quota excedida — segue sem persistir */
  }
}

const cache = loadCache()
let lastRequestAt = 0
// Fila serial: garante o intervalo mínimo entre chamadas ao Nominatim.
let queue: Promise<unknown> = Promise.resolve()

function normalizeKey(q: string): string {
  return q.trim().toLowerCase().replace(/\s+/g, ' ')
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

export const geocodingService = {
  /**
   * Converte um endereço em coordenadas usando o Nominatim (OpenStreetMap).
   * Resultados são cacheados em localStorage e as chamadas são serializadas
   * respeitando o limite de 1 req/s da API pública.
   */
  async geocode(query: string, signal?: AbortSignal): Promise<LatLng | null> {
    const key = normalizeKey(query)
    if (key in cache) return cache[key]

    const run = queue.then(async () => {
      if (key in cache) return cache[key]
      const wait = Math.max(0, MIN_INTERVAL_MS - (Date.now() - lastRequestAt))
      if (wait > 0) await new Promise((r) => setTimeout(r, wait))
      lastRequestAt = Date.now()
      try {
        const position = await requestNominatim(query, signal)
        cache[key] = position
        saveCache(cache)
        return position
      } catch {
        return null
      }
    })

    queue = run.catch(() => undefined)
    return run
  },
}
