import { beforeEach, describe, expect, it } from 'vitest'
import type { AuthResult } from '@/models/types/auth'
import { tokenStorage } from './tokenStorage'

const authResult: AuthResult = {
  userId: 'user-1',
  email: 'admin@doacao.local',
  fullName: 'Admin',
  roles: ['Admin'],
  accessToken: 'access-token',
  refreshToken: 'refresh-token',
  accessTokenExpiresAtUtc: '2026-01-01T00:00:00Z',
  refreshTokenExpiresAtUtc: '2026-02-01T00:00:00Z',
}

describe('tokenStorage', () => {
  beforeEach(() => {
    tokenStorage.clear()
    localStorage.clear()
  })

  it('retorna null quando não há sessão salva', () => {
    expect(tokenStorage.get()).toBeNull()
    expect(tokenStorage.getAccessToken()).toBeNull()
    expect(tokenStorage.getRefreshToken()).toBeNull()
  })

  it('persiste a sessão a partir do AuthResult e a expõe via get/getAccessToken/getRefreshToken', () => {
    const session = tokenStorage.save(authResult)

    expect(session.user).toEqual({
      userId: authResult.userId,
      email: authResult.email,
      fullName: authResult.fullName,
      roles: authResult.roles,
    })
    expect(tokenStorage.get()).toEqual(session)
    expect(tokenStorage.getAccessToken()).toBe('access-token')
    expect(tokenStorage.getRefreshToken()).toBe('refresh-token')
  })

  it('clear() remove a sessão do localStorage e do cache em memória', () => {
    tokenStorage.save(authResult)

    tokenStorage.clear()

    expect(tokenStorage.get()).toBeNull()
    expect(localStorage.getItem('doacao.auth.session')).toBeNull()
  })

  it('não lança e trata como sessão ausente quando o localStorage tem um JSON inválido', () => {
    localStorage.setItem('doacao.auth.session', '{isso não é json')

    expect(tokenStorage.get()).toBeNull()
  })
})
