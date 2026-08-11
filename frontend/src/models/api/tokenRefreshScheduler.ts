import { forceLogout, requestTokenRefresh } from './httpClient'
import { tokenStorage } from './tokenStorage'

const REFRESH_BEFORE_EXPIRY_MS = 60_000
const MIN_DELAY_MS = 5_000
const RETRY_AFTER_TRANSIENT_FAILURE_MS = 30_000

let timeoutId: ReturnType<typeof setTimeout> | null = null

function clearScheduledRefresh(): void {
  if (timeoutId !== null) {
    clearTimeout(timeoutId)
    timeoutId = null
  }
}

async function runScheduledRefresh(): Promise<void> {
  if (!tokenStorage.getRefreshToken()) return

  const result = await requestTokenRefresh()
  if (result.ok) {
    scheduleProactiveRefresh()
    return
  }

  if (result.reason === 'invalid') {
    forceLogout()
    return
  }

  scheduleAt(RETRY_AFTER_TRANSIENT_FAILURE_MS)
}

function scheduleAt(delayMs: number): void {
  clearScheduledRefresh()
  timeoutId = setTimeout(runScheduledRefresh, delayMs)
}

/**
 * Agenda a renovação do access token um pouco antes de expirar, evitando a
 * espera reativa por um 401 a cada janela de 4h. Chamar após login/refresh e
 * na inicialização do app, quando já existir sessão salva.
 */
export function scheduleProactiveRefresh(): void {
  const session = tokenStorage.get()
  if (!session) {
    clearScheduledRefresh()
    return
  }

  const expiresInMs = new Date(session.accessTokenExpiresAtUtc).getTime() - Date.now()
  scheduleAt(Math.max(expiresInMs - REFRESH_BEFORE_EXPIRY_MS, MIN_DELAY_MS))
}

export function stopProactiveRefresh(): void {
  clearScheduledRefresh()
}
