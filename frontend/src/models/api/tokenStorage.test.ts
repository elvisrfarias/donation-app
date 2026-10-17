import { beforeEach, describe, expect, it } from 'vitest'
import type { AuthResult } from '@/models/types/auth'
import { tokenStorage } from './tokenStorage'

const authResult: AuthResult = {
  userId: 'user-1',
  email: 'admin@doacao.local',
  fullName: 'Admin',
  roles: ['Admin'],
  collectionPointId: null,
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
      collectionPointId: authResult.collectionPointId,
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

  it('adota a sessão gravada por outra aba (evento storage) em vez do cache em memória', () => {
    tokenStorage.save(authResult)
    const rotatedJson = JSON.stringify({
      ...tokenStorage.get(),
      accessToken: 'access-2',
      refreshToken: 'refresh-2',
    })
    localStorage.setItem('doacao.auth.session', rotatedJson)

    window.dispatchEvent(
      new StorageEvent('storage', { key: 'doacao.auth.session', newValue: rotatedJson }),
    )

    expect(tokenStorage.getRefreshToken()).toBe('refresh-2')
  })

  it('reload() relê o localStorage mesmo com sessão em cache', () => {
    tokenStorage.save(authResult)
    localStorage.setItem(
      'doacao.auth.session',
      JSON.stringify({ ...tokenStorage.get(), refreshToken: 'refresh-de-outra-aba' }),
    )

    expect(tokenStorage.reload()?.refreshToken).toBe('refresh-de-outra-aba')
  })
})
