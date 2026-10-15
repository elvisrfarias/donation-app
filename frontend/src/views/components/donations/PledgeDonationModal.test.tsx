import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { donationService } from '@/models/services'
import type { CollectionPoint, Donation } from '@/models/types'
import { PledgeDonationModal } from './PledgeDonationModal'

const point = {
  id: 'point-a',
  name: 'Casa Solidária',
  openingHours: 'Seg a Sex, 9h às 17h',
  address: { street: 'Rua A', number: '10', city: 'Concórdia', state: 'SC' },
  contact: { phone: '49988887777' },
  needs: ['Leite em pó'],
  categories: [{ id: 'cat-1', name: 'Alimentos', icon: 'food' }],
} as unknown as CollectionPoint

const pledged: Donation = {
  id: 'donation-1',
  collectionPointId: 'point-a',
  donorName: 'Carlos',
  description: '2 cestas básicas',
  status: 'Pledged',
  createdAtUtc: '2026-06-21T12:00:00Z',
}

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderModal(onClose = vi.fn()) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <PledgeDonationModal point={point} onClose={onClose} />
    </QueryClientProvider>,
  )
  return onClose
}

describe('PledgeDonationModal', () => {
  it('não envia sem nome e mostra o erro', async () => {
    const pledge = vi.spyOn(donationService, 'pledge').mockResolvedValue(pledged)
    renderModal()

    await userEvent.click(screen.getByRole('button', { name: /avisar o ponto/i }))

    expect(await screen.findByText('Informe seu nome.')).toBeTruthy()
    expect(pledge).not.toHaveBeenCalled()
  })

  it('não envia sem descrição', async () => {
    const pledge = vi.spyOn(donationService, 'pledge').mockResolvedValue(pledged)
    renderModal()

    await userEvent.type(screen.getByLabelText(/seu nome/i), 'Carlos')
    await userEvent.click(screen.getByRole('button', { name: /avisar o ponto/i }))

    expect(await screen.findByText('Descreva o que você vai doar.')).toBeTruthy()
    expect(pledge).not.toHaveBeenCalled()
  })

  it('recusa telefone incompleto antes de enviar', async () => {
    const pledge = vi.spyOn(donationService, 'pledge').mockResolvedValue(pledged)
    renderModal()

    await userEvent.type(screen.getByLabelText(/seu nome/i), 'Carlos')
    await userEvent.type(screen.getByLabelText(/telefone/i), '1198')
    await userEvent.type(screen.getByLabelText(/o que você vai doar/i), 'Roupas')
    await userEvent.click(screen.getByRole('button', { name: /avisar o ponto/i }))

    expect(await screen.findByText(/Telefone inválido/i)).toBeTruthy()
    expect(pledge).not.toHaveBeenCalled()
  })

  it('preenche a descrição ao tocar numa sugestão do ponto', async () => {
    renderModal()

    await userEvent.click(screen.getByRole('button', { name: /adicionar leite em pó/i }))
    await userEvent.click(screen.getByRole('button', { name: /adicionar alimentos/i }))

    const description = screen.getByLabelText(/o que você vai doar/i) as HTMLTextAreaElement
    expect(description.value).toBe('Leite em pó, Alimentos')
  })

  it('oferece o WhatsApp do ponto depois de avisar', async () => {
    vi.spyOn(donationService, 'pledge').mockResolvedValue(pledged)
    renderModal()

    await userEvent.type(screen.getByLabelText(/seu nome/i), 'Carlos')
    await userEvent.type(screen.getByLabelText(/o que você vai doar/i), 'Roupas')
    await userEvent.click(screen.getByRole('button', { name: /avisar o ponto/i }))

    const whatsapp = await screen.findByRole('link', { name: /combinar entrega pelo whatsapp/i })
    expect(whatsapp.getAttribute('href')).toBe('https://wa.me/5549988887777')
  })

  it('envia os dados limpos e mostra a confirmação', async () => {
    const pledge = vi.spyOn(donationService, 'pledge').mockResolvedValue(pledged)
    renderModal()

    await userEvent.type(screen.getByLabelText(/seu nome/i), '  Carlos ')
    await userEvent.type(screen.getByLabelText(/telefone/i), '11987654321')
    await userEvent.type(screen.getByLabelText(/o que você vai doar/i), '2 cestas básicas')
    await userEvent.click(screen.getByRole('button', { name: /avisar o ponto/i }))

    await waitFor(() =>
      expect(pledge).toHaveBeenCalledWith('point-a', {
        donorName: 'Carlos',
        donorPhone: '(11) 98765-4321',
        description: '2 cestas básicas',
      }),
    )
    expect(await screen.findByText(/Obrigado por ajudar/i)).toBeTruthy()
  })
})
