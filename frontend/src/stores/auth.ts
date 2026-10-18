import { create } from 'zustand'
import { setForcedLogoutHandler } from '@/models/api/httpClient'
import { scheduleProactiveRefresh, stopProactiveRefresh } from '@/models/api/tokenRefreshScheduler'
import { tokenStorage } from '@/models/api/tokenStorage'
import { authService } from '@/models/services/authService'
import type { AuthResult, AuthUser, Role } from '@/models/types/auth'

type AuthStatus = 'authenticated' | 'unauthenticated'

const SESSION_EXPIRED_MESSAGE = 'Sua sessão expirou. Faça login novamente.'

interface AuthState {
  user: AuthUser | null
  status: AuthStatus
  sessionExpiredMessage: string | null
  setSession: (result: AuthResult) => void
  logout: () => Promise<void>
  hasRole: (...roles: Role[]) => boolean
  clearSessionExpiredMessage: () => void
}

/**
 * Estado de autenticação reativo. A persistência fica no `tokenStorage` (Model);
 * este store é a fonte para a UI e deriva o estado inicial do que estiver salvo.
 */
export const useAuthStore = create<AuthState>((set, get) => {
  const existing = tokenStorage.get()
  if (existing) scheduleProactiveRefresh()

  return {
    user: existing?.user ?? null,
    status: existing ? 'authenticated' : 'unauthenticated',
    sessionExpiredMessage: null,

    setSession: (result) => {
      const session = tokenStorage.save(result)
      scheduleProactiveRefresh()
      set({ user: session.user, status: 'authenticated', sessionExpiredMessage: null })
    },

    logout: async () => {
      const refreshToken = tokenStorage.getRefreshToken()
      if (refreshToken) {
        try {
          await authService.revoke(refreshToken)
        } catch {}
      }
      stopProactiveRefresh()
      tokenStorage.clear()
      set({ user: null, status: 'unauthenticated' })
    },

    hasRole: (...roles) => {
      const user = get().user
      return !!user && roles.some((r) => user.roles.includes(r))
    },

    clearSessionExpiredMessage: () => set({ sessionExpiredMessage: null }),
  }
})

setForcedLogoutHandler(() => {
  stopProactiveRefresh()
  tokenStorage.clear()
  useAuthStore.setState({ user: null, status: 'unauthenticated', sessionExpiredMessage: SESSION_EXPIRED_MESSAGE })
})
