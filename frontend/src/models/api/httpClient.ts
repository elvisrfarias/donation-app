import axios, { type InternalAxiosRequestConfig } from 'axios'
import { env } from '@/lib/env'
import type { AuthResult } from '@/models/types/auth'
import { toApiError } from './problemDetails'
import { tokenStorage } from './tokenStorage'

type RetriableConfig = InternalAxiosRequestConfig & { _retry?: boolean }

/** Cliente HTTP único do app. Anexa o JWT e renova tokens de forma transparente. */
export const http = axios.create({
  baseURL: env.apiUrl,
  headers: { 'Content-Type': 'application/json' },
})

http.interceptors.request.use((config) => {
  const token = tokenStorage.getAccessToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Permite que a camada de estado reaja a um logout forçado (refresh falhou).
let onForcedLogout: (() => void) | null = null
export function setForcedLogoutHandler(cb: () => void): void {
  onForcedLogout = cb
}

// Single-flight: vários 401 simultâneos disparam um único refresh.
let refreshing: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = tokenStorage.getRefreshToken()
  if (!refreshToken) return null
  try {
    // Instância "crua" (sem interceptors) para não recursar no 401.
    const { data } = await axios.post<AuthResult>(`${env.apiUrl}/auth/refresh`, { refreshToken })
    tokenStorage.save(data)
    return data.accessToken
  } catch {
    return null
  }
}

http.interceptors.response.use(
  (response) => response,
  async (error) => {
    const config = error?.config as RetriableConfig | undefined
    const status = error?.response?.status

    if (status === 401 && config && !config._retry && tokenStorage.getRefreshToken()) {
      config._retry = true
      refreshing ??= refreshAccessToken().finally(() => {
        refreshing = null
      })
      const newToken = await refreshing
      if (newToken) {
        config.headers.Authorization = `Bearer ${newToken}`
        return http(config)
      }
      tokenStorage.clear()
      onForcedLogout?.()
    }

    return Promise.reject(toApiError(error))
  },
)
