import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/models/api/problemDetails'
import { authService } from '@/models/services'
import { ResetPasswordPage } from './ResetPasswordPage'

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderPage(initialEntry: string) {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[initialEntry]}>
        <ResetPasswordPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

async function fillNewPassword(password: string, confirmation = password) {
  await userEvent.type(screen.getByLabelText(/^Nova senha/), password)
  await userEvent.type(screen.getByLabelText(/^Confirme a nova senha/), confirmation)
  await userEvent.click(screen.getByRole('button', { name: /Salvar nova senha/ }))
}

describe('ResetPasswordPage', () => {
  it('envia o token do link junto com a nova senha', async () => {
    const completePasswordReset = vi
      .spyOn(authService, 'completePasswordReset')
      .mockResolvedValue(undefined)
    renderPage('/reset-password#token=abc-123_XYZ')

    await fillNewPassword('SenhaNova2')

    expect(completePasswordReset).toHaveBeenCalledWith({
      token: 'abc-123_XYZ',
      newPassword: 'SenhaNova2',
    })
    expect(await screen.findByText(/Senha redefinida!/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Entrar com a nova senha/ })).toBeInTheDocument()
  })

  it('recusa senhas que não conferem antes de chamar a API', async () => {
    const completePasswordReset = vi.spyOn(authService, 'completePasswordReset')
    renderPage('/reset-password#token=abc')

    await fillNewPassword('SenhaNova2', 'SenhaNova3')

    expect(await screen.findByText('As senhas não conferem.')).toBeInTheDocument()
    expect(completePasswordReset).not.toHaveBeenCalled()
  })

  it('mostra o erro de link expirado e oferece pedir outro', async () => {
    vi.spyOn(authService, 'completePasswordReset').mockRejectedValue(
      new ApiError({
        status: 400,
        title: 'One or more validation errors occurred.',
        fieldErrors: {
          'Auth.InvalidPasswordResetToken': [
            'Este link de redefinição é inválido ou expirou. Peça um novo link.',
          ],
        },
      }),
    )
    renderPage('/reset-password#token=usado')

    await fillNewPassword('SenhaNova2')

    expect(await screen.findByText(/inválido ou expirou/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Pedir um novo link' })).toHaveAttribute(
      'href',
      '/forgot-password',
    )
  })

  it('avisa quando o link chega sem token', () => {
    renderPage('/reset-password')

    expect(screen.getByText(/link de redefinição está incompleto/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Salvar nova senha/ })).not.toBeInTheDocument()
  })
})
