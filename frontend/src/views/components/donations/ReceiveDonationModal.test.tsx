import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { collectionPointService, donationService } from '@/models/services'
import type { CollectionPoint, Donation } from '@/models/types'
import { ReceiveDonationModal } from './ReceiveDonationModal'

const donation: Donation = {
  id: 'donation-1',
  collectionPointId: 'point-a',
  donorName: 'Carlos',
  description: '10kg de arroz',
  status: 'Pledged',
  createdAtUtc: '2026-06-21T12:00:00Z',
}

const point = {
  id: 'point-a',
  stockItems: [
    { id: 'item-rice', name: 'Arroz', unit: 'kg', quantity: 10 },
    { id: 'item-beans', name: 'Feijão', unit: 'kg', quantity: 5 },
  ],
} as unknown as CollectionPoint

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderModal(onClose = vi.fn()) {
  vi.spyOn(collectionPointService, 'getById').mockResolvedValue(point)
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <ReceiveDonationModal pointId="point-a" donation={donation} onClose={onClose} />
    </QueryClientProvider>,
  )
  return onClose
}

describe('ReceiveDonationModal', () => {
  it('envia só os itens com quantidade positiva', async () => {
    const receive = vi
      .spyOn(donationService, 'receive')
      .mockResolvedValue({ ...donation, status: 'Received' })
    const onClose = renderModal()

    await userEvent.type(await screen.findByLabelText(/arroz/i), '4')
    await userEvent.type(screen.getByLabelText(/feijão/i), '0')
    await userEvent.click(screen.getByRole('button', { name: /confirmar recebimento/i }))

    await waitFor(() =>
      expect(receive).toHaveBeenCalledWith('point-a', 'donation-1', {
        receivedStock: [{ stockItemId: 'item-rice', quantity: 4 }],
      }),
    )
    await waitFor(() => expect(onClose).toHaveBeenCalled())
  })

  it('mostra quanto o estoque passa a ter ao digitar a quantidade recebida', async () => {
    renderModal()

    await userEvent.type(await screen.findByLabelText(/arroz/i), '4')

    expect(screen.getByText(/passa a 14/i)).toBeTruthy()
  })

  it('sem quantidades, envia lista vazia (só marca como recebida)', async () => {
    const receive = vi
      .spyOn(donationService, 'receive')
      .mockResolvedValue({ ...donation, status: 'Received' })
    renderModal()

    await userEvent.click(await screen.findByRole('button', { name: /confirmar recebimento/i }))

    await waitFor(() =>
      expect(receive).toHaveBeenCalledWith('point-a', 'donation-1', { receivedStock: [] }),
    )
  })

  it('mostra o erro da API e não fecha', async () => {
    vi.spyOn(donationService, 'receive').mockRejectedValue({
      displayMessage: 'Esta doação já foi recebida ou cancelada.',
    })
    const onClose = renderModal()

    await userEvent.click(await screen.findByRole('button', { name: /confirmar recebimento/i }))

    expect(await screen.findByText('Esta doação já foi recebida ou cancelada.')).toBeTruthy()
    expect(onClose).not.toHaveBeenCalled()
  })
})
