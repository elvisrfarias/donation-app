import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { geocodingService } from './geocodingService'

function nominatimResponse(lat: string, lon: string) {
  return { ok: true, json: async () => [{ lat, lon }] } as Response
}

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

describe('geocodingService.geocode', () => {
  it('atende primeiro a consulta urgente, mesmo chegando depois das normais', async () => {
    const requestedQueries: string[] = []
    vi.stubGlobal(
      'fetch',
      vi.fn(async (url: URL) => {
        requestedQueries.push(url.searchParams.get('q') ?? '')
        return nominatimResponse('-27.2', '-52.0')
      }),
    )

    const first = geocodingService.geocode('prioridade-a rua 1')
    const second = geocodingService.geocode('prioridade-b rua 2')
    const third = geocodingService.geocode('prioridade-c rua 3')
    const urgent = geocodingService.geocode('prioridade-urgente rua 9', { urgent: true })

    await vi.runAllTimersAsync()
    await Promise.all([first, second, third, urgent])

    expect(requestedQueries[0]).toBe('prioridade-a rua 1')
    expect(requestedQueries[1]).toBe('prioridade-urgente rua 9')
    expect(requestedQueries.slice(2)).toEqual(['prioridade-b rua 2', 'prioridade-c rua 3'])
  })

  it('devolve o resultado do cache sem nova requisição', async () => {
    const fetchMock = vi.fn(async () => nominatimResponse('-27.3', '-52.1'))
    vi.stubGlobal('fetch', fetchMock)

    const firstLookup = geocodingService.geocode('cache-teste rua 5')
    await vi.runAllTimersAsync()
    await firstLookup
    const secondLookup = await geocodingService.geocode('Cache-Teste  Rua 5')

    expect(secondLookup).toEqual({ lat: -27.3, lng: -52.1 })
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('devolve null quando a API falha, sem derrubar a fila', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => ({ ok: false, status: 429 }) as Response),
    )

    const lookup = geocodingService.geocode('falha-teste rua 7', { urgent: true })
    await vi.runAllTimersAsync()

    expect(await lookup).toBeNull()
  })
})
