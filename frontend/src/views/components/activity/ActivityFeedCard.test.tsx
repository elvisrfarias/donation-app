import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { activityService } from '@/models/services'
import { ActivityFeedCard } from './ActivityFeedCard'

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderCard() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <ActivityFeedCard pointId="point-a" />
    </QueryClientProvider>,
  )
}

describe('ActivityFeedCard', () => {
  it('lista os acontecimentos do ponto', async () => {
    vi.spyOn(activityService, 'getByPoint').mockResolvedValue({
      items: [
        {
          kind: 'LowStock',
          title: 'Estoque baixo: Leite',
          description: 'Restam 2 L (aviso em 5).',
          occurredAtUtc: '2026-06-22T12:00:00Z',
        },
        {
          kind: 'Donation',
          title: 'Doação de Carlos',
          description: '10kg de arroz — aguardando entrega.',
          occurredAtUtc: '2026-06-21T12:00:00Z',
        },
      ],
    })
    renderCard()

    expect(await screen.findByText('Estoque baixo: Leite')).toBeTruthy()
    expect(screen.getByText('Doação de Carlos')).toBeTruthy()
  })

  it('avisa quando não há nada de novo', async () => {
    vi.spyOn(activityService, 'getByPoint').mockResolvedValue({ items: [] })
    renderCard()

    expect(await screen.findByText('Nada de novo por aqui ainda.')).toBeTruthy()
  })
})
