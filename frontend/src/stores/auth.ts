import { create } from 'zustand'
import { setForcedLogoutHandler } from '@/models/api/httpClient'
import { tokenStorage } from '@/models/api/tokenStorage'
import { authService } from '@/models/services'
import type { AuthResult, AuthUser, Role } from '@/models/types/auth'

type AuthStatus = 'authenticated' | 'unauthenticated'

interface AuthState {
  user: AuthUser | null
  status: AuthStatus
  setSession: (result: AuthResult) => void
  logout: () => Promise<void>
  hasRole: (...roles: Role[]) => boolean
}

/**
 * Estado de autenticação reativo. A persistência fica no `tokenStorage` (Model);
 * este store é a fonte para a UI e deriva o estado inicial do que estiver salvo.
 */
export const useAuthStore = create<AuthState>((set, get) => {
  const existing = tokenStorage.get()

  return {
    user: existing?.user ?? null,
    status: existing ? 'authenticated' : 'unauthenticated',

    setSession: (result) => {
      const session = tokenStorage.save(result)
      set({ user: session.user, status: 'authenticated' })
    },

    logout: async () => {
      const refreshToken = tokenStorage.getRefreshToken()
      if (refreshToken) {
        try {
          await authService.revoke(refreshToken)
        } catch {}
      }
      tokenStorage.clear()
      set({ user: null, status: 'unauthenticated' })
    },

    hasRole: (...roles) => {
      const user = get().user
      return !!user && roles.some((r) => user.roles.includes(r))
    },
  }
})

setForcedLogoutHandler(() => {
  tokenStorage.clear()
  useAuthStore.setState({ user: null, status: 'unauthenticated' })
})
