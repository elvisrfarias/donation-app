import { z } from 'zod'

/**
 * Configuração de ambiente validada na inicialização. Falhar cedo é melhor do que
 * um `undefined` silencioso espalhado pelo app.
 */
const schema = z.object({
  VITE_API_URL: z.string().default('/api'),
  VITE_NOMINATIM_URL: z.string().default('https://nominatim.openstreetmap.org'),
  VITE_MAP_DEFAULT_LAT: z.coerce.number().default(-15.78),
  VITE_MAP_DEFAULT_LNG: z.coerce.number().default(-47.92),
  VITE_MAP_DEFAULT_ZOOM: z.coerce.number().int().min(1).max(19).default(4),
})

const parsed = schema.safeParse(import.meta.env)

if (!parsed.success) {
  console.error('Variáveis de ambiente inválidas:', parsed.error.issues)
  throw new Error('Configuração de ambiente inválida — verifique o arquivo .env do frontend.')
}

const data = parsed.data

export const env = {
  apiUrl: data.VITE_API_URL.replace(/\/$/, ''),
  nominatimUrl: data.VITE_NOMINATIM_URL.replace(/\/$/, ''),
  map: {
    defaultCenter: [data.VITE_MAP_DEFAULT_LAT, data.VITE_MAP_DEFAULT_LNG] as [number, number],
    defaultZoom: data.VITE_MAP_DEFAULT_ZOOM,
  },
} as const
