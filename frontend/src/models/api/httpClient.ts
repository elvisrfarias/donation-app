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

let onForcedLogout: (() => void) | null = null
export function setForcedLogoutHandler(cb: () => void): void {
  onForcedLogout = cb
}

export function forceLogout(): void {
  tokenStorage.clear()
  onForcedLogout?.()
}

export type RefreshOutcome =
  | { ok: true; accessToken: string }
  /** "invalid": o refresh token foi rejeitado (expirado/revogado) — sessão deve encerrar.
   *  "transient": falha de rede ou erro de servidor — a sessão continua válida, tenta de novo depois. */
  | { ok: false; reason: 'invalid' | 'transient' }

let refreshing: Promise<RefreshOutcome> | null = null

/** Outra aba pode ter rotacionado o refresh token enquanto este ainda estava em uso: adota a sessão dela. */
function sessionRotatedByAnotherTab(usedRefreshToken: string): RefreshOutcome | null {
  const latest = tokenStorage.reload()
  return latest && latest.refreshToken !== usedRefreshToken
    ? { ok: true, accessToken: latest.accessToken }
    : null
}

async function refreshAccessToken(): Promise<RefreshOutcome> {
  const refreshToken = tokenStorage.getRefreshToken()
  if (!refreshToken) return { ok: false, reason: 'invalid' }
  try {
    const { data } = await axios.post<AuthResult>(`${env.apiUrl}/auth/refresh`, { refreshToken })
    tokenStorage.save(data)
    return { ok: true, accessToken: data.accessToken }
  } catch (error) {
    const status = toApiError(error).status
    if (status === 0 || status >= 500) return { ok: false, reason: 'transient' }
    return sessionRotatedByAnotherTab(refreshToken) ?? { ok: false, reason: 'invalid' }
  }
}

/** Dispara (ou reaproveita, se já em andamento) uma renovação do access token. */
export function requestTokenRefresh(): Promise<RefreshOutcome> {
  refreshing ??= refreshAccessToken().finally(() => {
    refreshing = null
  })
  return refreshing
}

http.interceptors.response.use(
  (response) => response,
  async (error) => {
    const config = error?.config as RetriableConfig | undefined
    const status = error?.response?.status

    if (status === 401 && config && !config._retry && tokenStorage.getRefreshToken()) {
      config._retry = true
      const result = await requestTokenRefresh()
      if (result.ok) {
        config.headers.Authorization = `Bearer ${result.accessToken}`
        return http(config)
      }
      if (result.reason === 'invalid') {
        forceLogout()
      }
    }

    return Promise.reject(toApiError(error))
  },
)
