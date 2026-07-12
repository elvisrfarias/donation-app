import { env } from '@/lib/env'
import { logger } from '@/lib/logger'
import type { LatLng } from '@/models/types'

/** Endereço resolvido a partir de um CEP (BrasilAPI). */
export interface CepLookup {
  street: string
  district: string
  city: string
  state: string
  zipCode: string
  /** Coordenadas aproximadas do CEP, quando o provedor as fornece. */
  position: LatLng | null
}

interface BrasilApiCepResponse {
  cep: string
  state: string
  city: string
  neighborhood?: string
  street?: string
  location?: {
    coordinates?: {
      latitude?: string | number
      longitude?: string | number
    }
  }
}

function onlyDigits(value: string): string {
  return value.replace(/\D/g, '')
}

function parsePosition(response: BrasilApiCepResponse): LatLng | null {
  const coords = response.location?.coordinates
  if (coords?.latitude == null || coords.longitude == null) return null
  const lat = Number(coords.latitude)
  const lng = Number(coords.longitude)
  if (Number.isNaN(lat) || Number.isNaN(lng)) return null
  return { lat, lng }
}

export const cepService = {
  /**
   * Busca o endereço de um CEP na BrasilAPI. Usado para autopreencher os campos do
   * ponto de coleta e como fallback de coordenadas quando o Nominatim não localiza.
   * Retorna `null` quando o CEP é inválido ou não encontrado.
   */
  async lookup(cep: string, signal?: AbortSignal): Promise<CepLookup | null> {
    const digits = onlyDigits(cep)
    if (digits.length !== 8) return null

    try {
      const res = await fetch(`${env.brasilApiUrl}/api/cep/v2/${digits}`, {
        signal,
        headers: { Accept: 'application/json' },
      })
      if (!res.ok) return null

      const data = (await res.json()) as BrasilApiCepResponse
      return {
        street: data.street?.trim() ?? '',
        district: data.neighborhood?.trim() ?? '',
        city: data.city?.trim() ?? '',
        state: data.state?.trim() ?? '',
        zipCode: digits,
        position: parsePosition(data),
      }
    } catch (error) {
      logger.warn('Falha ao consultar o CEP na BrasilAPI; seguindo sem autopreenchimento.', {
        cep: digits,
        error,
      })
      return null
    }
  },
}
