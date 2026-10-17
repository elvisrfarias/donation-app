import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { stopProactiveRefresh } from '@/models/api/tokenRefreshScheduler'
import { tokenStorage } from '@/models/api/tokenStorage'
import { authService } from '@/models/services'
import type { AuthResult } from '@/models/types'
import { useAuthStore } from '@/stores/auth'
import { ChangePasswordModal } from './ChangePasswordModal'

const ONE_HOUR_MS = 60 * 60 * 1000

const renewedSession: AuthResult = {
  userId: 'user-1',
  email: 'gestor@doacao.local',
  fullName: 'Gestor',
  roles: ['PointManager'],
  collectionPointId: 'point-a',
  accessToken: 'novo-access',
  refreshToken: 'novo-refresh',
  accessTokenExpiresAtUtc: new Date(Date.now() + ONE_HOUR_MS).toISOString(),
  refreshTokenExpiresAtUtc: new Date(Date.now() + 2 * ONE_HOUR_MS).toISOString(),
}

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  stopProactiveRefresh()
  tokenStorage.clear()
  useAuthStore.setState({ user: null, status: 'unauthenticated' })
})

function renderModal() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <ChangePasswordModal open onOpenChange={vi.fn()} />
    </QueryClientProvider>,
  )
}

async function fillAndSubmit(currentPassword: string, newPassword: string, confirmPassword: string) {
  await userEvent.type(screen.getByLabelText(/senha atual/i), currentPassword)
  await userEvent.type(screen.getByLabelText(/^nova senha/i), newPassword)
  await userEvent.type(screen.getByLabelText(/confirme a nova senha/i), confirmPassword)
  await userEvent.click(screen.getByRole('button', { name: /trocar senha/i }))
}

describe('ChangePasswordModal', () => {
  it('troca a senha e adota os tokens novos da sessão', async () => {
    const changePassword = vi.spyOn(authService, 'changePassword').mockResolvedValue(renewedSession)
    renderModal()

    await fillAndSubmit('SenhaAtual1', 'SenhaNova2', 'SenhaNova2')

    await waitFor(() =>
      expect(changePassword).toHaveBeenCalledWith({
        currentPassword: 'SenhaAtual1',
        newPassword: 'SenhaNova2',
      }),
    )
    expect(await screen.findByText(/senha alterada com sucesso/i)).toBeInTheDocument()
    expect(useAuthStore.getState().user?.userId).toBe('user-1')
    expect(tokenStorage.getRefreshToken()).toBe('novo-refresh')
  })

  it('não envia quando a confirmação não confere', async () => {
    const changePassword = vi.spyOn(authService, 'changePassword')
    renderModal()

    await fillAndSubmit('SenhaAtual1', 'SenhaNova2', 'SenhaDiferente3')

    expect(await screen.findByText(/as senhas não conferem/i)).toBeInTheDocument()
    expect(changePassword).not.toHaveBeenCalled()
  })
})
