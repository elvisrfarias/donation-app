import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { noticeService } from '@/models/services'
import type { Notice } from '@/models/types'
import { useAuthStore } from '@/stores/auth'
import { NoticesPage } from './NoticesPage'

const ownNotice: Notice = {
  id: 'notice-own',
  collectionPointId: 'point-a',
  pointName: 'Casa Solidária',
  title: 'Falta leite',
  message: 'Precisamos de leite em pó',
  audience: 'Public',
  authorName: 'Casa Solidária',
  createdAtUtc: '2026-06-21T12:00:00Z',
}

const otherNotice: Notice = {
  ...ownNotice,
  id: 'notice-other',
  collectionPointId: 'point-b',
  pointName: 'Paróquia São José',
  title: 'Mutirão sábado',
  audience: 'Managers',
}

beforeEach(() => {
  useAuthStore.setState({
    user: {
      userId: 'user-1',
      email: 'gestor@x.com',
      fullName: 'Gestor',
      roles: ['PointManager'],
      collectionPointId: 'point-a',
    },
  })
  vi.spyOn(noticeService, 'listNetwork').mockResolvedValue([ownNotice, otherNotice])
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <NoticesPage />
    </QueryClientProvider>,
  )
}

describe('NoticesPage', () => {
  it('mostra o mural da rede e só deixa retirar os avisos do próprio ponto', async () => {
    renderPage()

    expect(await screen.findByText('Falta leite')).toBeTruthy()
    expect(screen.getByText('Mutirão sábado')).toBeTruthy()
    expect(screen.getByRole('button', { name: /retirar o aviso falta leite/i })).toBeTruthy()
    expect(screen.queryByRole('button', { name: /retirar o aviso mutirão sábado/i })).toBeNull()
  })

  it('publica o aviso com o público escolhido', async () => {
    const publish = vi.spyOn(noticeService, 'publish').mockResolvedValue(ownNotice)
    renderPage()

    await userEvent.type(await screen.findByLabelText(/título/i), 'Doação de agasalhos')
    await userEvent.type(screen.getByLabelText(/mensagem/i), 'Aceitamos agasalhos infantis')
    await userEvent.click(screen.getByLabelText(/só gestores/i))
    await userEvent.click(screen.getByRole('button', { name: /publicar aviso/i }))

    await waitFor(() =>
      expect(publish).toHaveBeenCalledWith('point-a', {
        title: 'Doação de agasalhos',
        message: 'Aceitamos agasalhos infantis',
        audience: 'Managers',
      }),
    )
  })

  it('mantém o botão de publicar desabilitado enquanto faltar título ou mensagem', async () => {
    renderPage()

    const button = await screen.findByRole('button', { name: /publicar aviso/i })

    expect((button as HTMLButtonElement).disabled).toBe(true)
  })
})
