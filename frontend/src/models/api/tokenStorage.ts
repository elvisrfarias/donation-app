import type { AuthResult, AuthUser } from '@/models/types/auth'

const STORAGE_KEY = 'doacao.auth.session'

export interface AuthSession {
  user: AuthUser
  accessToken: string
  refreshToken: string
  accessTokenExpiresAtUtc: string
  refreshTokenExpiresAtUtc: string
}

function fromAuthResult(r: AuthResult): AuthSession {
  return {
    user: { userId: r.userId, email: r.email, fullName: r.fullName, roles: r.roles },
    accessToken: r.accessToken,
    refreshToken: r.refreshToken,
    accessTokenExpiresAtUtc: r.accessTokenExpiresAtUtc,
    refreshTokenExpiresAtUtc: r.refreshTokenExpiresAtUtc,
  }
}

// Cache em memória para evitar JSON.parse a cada request do interceptor.
let cache: AuthSession | null | undefined

/**
 * Persistência da sessão de autenticação (Model). O store Zustand é a camada
 * reativa por cima disto; o interceptor HTTP lê os tokens daqui diretamente.
 */
export const tokenStorage = {
  get(): AuthSession | null {
    if (cache !== undefined) return cache
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      cache = raw ? (JSON.parse(raw) as AuthSession) : null
    } catch {
      cache = null
    }
    return cache
  },

  save(result: AuthResult): AuthSession {
    const session = fromAuthResult(result)
    cache = session
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
    } catch {
      /* storage indisponível (modo privado etc.) — mantém só em memória */
    }
    return session
  },

  clear(): void {
    cache = null
    try {
      localStorage.removeItem(STORAGE_KEY)
    } catch {
      /* ignore */
    }
  },

  getAccessToken(): string | null {
    return this.get()?.accessToken ?? null
  },

  getRefreshToken(): string | null {
    return this.get()?.refreshToken ?? null
  },
}
