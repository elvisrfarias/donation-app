import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { donationService } from '@/models/services'
import type { Donation } from '@/models/types'
import { useAuthStore } from '@/stores/auth'
import { DonationsPage } from './DonationsPage'

const pending: Donation = {
  id: 'donation-1',
  collectionPointId: 'point-a',
  donorName: 'Carlos',
  donorPhone: '(11) 98765-4321',
  description: '10kg de arroz',
  status: 'Pledged',
  createdAtUtc: '2026-06-21T12:00:00Z',
}

const received: Donation = { ...pending, id: 'donation-2', donorName: 'Ana', status: 'Received' }

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
  vi.spyOn(donationService, 'listByPoint').mockResolvedValue([pending, received])
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <DonationsPage />
    </QueryClientProvider>,
  )
}

describe('DonationsPage', () => {
  it('abre em "Aguardando" e troca de aba para ver as recebidas', async () => {
    renderPage()

    expect(await screen.findByText('Carlos')).toBeTruthy()
    expect(screen.queryByText('Ana')).toBeNull()
    expect(screen.getAllByRole('button', { name: /^recebi$/i })).toHaveLength(1)

    await userEvent.click(screen.getByRole('tab', { name: /recebidas/i }))

    expect(screen.getByText('Ana')).toBeTruthy()
    expect(screen.queryByRole('button', { name: /^recebi$/i })).toBeNull()
  })

  it('mostra a mensagem certa quando a aba está vazia', async () => {
    renderPage()

    await userEvent.click(await screen.findByRole('tab', { name: /canceladas/i }))

    expect(screen.getByText('Nenhuma doação cancelada')).toBeTruthy()
  })

  it('pede confirmação antes de cancelar e permite voltar', async () => {
    const cancel = vi.spyOn(donationService, 'cancel').mockResolvedValue({
      ...pending,
      status: 'Cancelled',
    })
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: /^cancelar$/i }))
    expect(cancel).not.toHaveBeenCalled()

    await userEvent.click(screen.getByRole('button', { name: /voltar/i }))
    expect(screen.getByRole('button', { name: /^cancelar$/i })).toBeTruthy()
    expect(cancel).not.toHaveBeenCalled()
  })

  it('cancela só depois da confirmação', async () => {
    const cancel = vi.spyOn(donationService, 'cancel').mockResolvedValue({
      ...pending,
      status: 'Cancelled',
    })
    renderPage()

    await userEvent.click(await screen.findByRole('button', { name: /^cancelar$/i }))
    await userEvent.click(screen.getByRole('button', { name: /confirmar cancelamento/i }))

    await waitFor(() => expect(cancel).toHaveBeenCalledWith('point-a', 'donation-1'))
  })
})
