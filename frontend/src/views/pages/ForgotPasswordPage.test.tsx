import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/models/api/problemDetails'
import { authService } from '@/models/services'
import { ForgotPasswordPage } from './ForgotPasswordPage'

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderPage() {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <ForgotPasswordPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('ForgotPasswordPage', () => {
  it('pede o link para o e-mail e confirma sem revelar se a conta existe', async () => {
    const requestPasswordReset = vi
      .spyOn(authService, 'requestPasswordReset')
      .mockResolvedValue(undefined)
    renderPage()

    await userEvent.type(screen.getByLabelText(/E-mail/), 'gestor@dooa.com.br')
    await userEvent.click(screen.getByRole('button', { name: /Enviar link/ }))

    expect(requestPasswordReset).toHaveBeenCalledWith({ email: 'gestor@dooa.com.br' })
    expect(await screen.findByText(/Se houver uma conta com o e-mail/)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Enviar link/ })).not.toBeInTheDocument()
  })

  it('não envia um e-mail inválido', async () => {
    const requestPasswordReset = vi.spyOn(authService, 'requestPasswordReset')
    renderPage()

    await userEvent.type(screen.getByLabelText(/E-mail/), 'nao-e-email')
    await userEvent.click(screen.getByRole('button', { name: /Enviar link/ }))

    expect(await screen.findByText('Informe um e-mail válido.')).toBeInTheDocument()
    expect(requestPasswordReset).not.toHaveBeenCalled()
  })

  it('mostra o erro quando o pedido falha, sem fingir que o link foi enviado', async () => {
    vi.spyOn(authService, 'requestPasswordReset').mockRejectedValue(
      new ApiError({ status: 429, title: 'Muitas tentativas. Aguarde alguns minutos.' }),
    )
    renderPage()

    await userEvent.type(screen.getByLabelText(/E-mail/), 'gestor@dooa.com.br')
    await userEvent.click(screen.getByRole('button', { name: /Enviar link/ }))

    expect(
      await screen.findByText('Muitas tentativas. Aguarde alguns minutos.'),
    ).toBeInTheDocument()
    expect(screen.queryByText(/Se houver uma conta/)).not.toBeInTheDocument()
  })
})
