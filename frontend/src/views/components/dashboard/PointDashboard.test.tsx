import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { currentMonthPeriod } from '@/lib/dashboardPeriod'
import { isoToBrazilian } from '@/lib/dateInput'
import { collectionPointService } from '@/models/services'
import type { CollectionPoint, CollectionPointDashboard } from '@/models/types'
import { PointDashboard } from './PointDashboard'

const point = {
  id: 'point-1',
  name: 'Casa Solidária',
  type: 'Association',
  address: { city: 'Concórdia', state: 'SC' },
  stock: [{ category: 'Food', quantity: 12 }],
  stockSettings: [{ category: 'Food', goal: 50, alertThreshold: 10, isTracked: true }],
  withdrawalRule: { type: 'Annual', maxWithdrawalsPerCycle: 3 },
  averageRating: 4.5,
  reviewCount: 2,
} as CollectionPoint

const dashboard: CollectionPointDashboard = {
  pointId: 'point-1',
  periodStart: '2026-06-01',
  periodEnd: '2026-06-30',
  withdrawalsInPeriod: 4,
  beneficiariesServedInPeriod: 3,
  totalWithdrawals: 25,
  registeredBeneficiaries: 20,
  granularity: 'Day',
  withdrawalsSeries: [
    { date: '2026-06-01', count: 1 },
    { date: '2026-06-02', count: 3 },
  ],
  recentWithdrawals: [
    { withdrawalId: 'w-1', beneficiaryName: 'Maria Souza', categories: ['Food', 'Clothing'], occurredAtUtc: '2026-06-20T14:00:00Z' },
  ],
}

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function mockServices() {
  vi.spyOn(collectionPointService, 'getById').mockResolvedValue(point)
  return vi.spyOn(collectionPointService, 'getDashboard').mockResolvedValue(dashboard)
}

function renderDashboard(pointId: string | null = 'point-1') {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <PointDashboard pointId={pointId} title="Olá, Gestor" />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('PointDashboard', () => {
  it('mostra só os indicadores do próprio ponto', async () => {
    mockServices()
    renderDashboard()

    expect(await screen.findByText('Retiradas no período')).toBeInTheDocument()
    expect(screen.getByText('Casa Solidária · Concórdia/SC')).toBeInTheDocument()
    expect(screen.getByText('25 desde o início do ponto')).toBeInTheDocument()
    expect(screen.getByText('no período · 20 cadastrados por este ponto')).toBeInTheDocument()
    expect(screen.getByText('Maria Souza')).toBeInTheDocument()
    expect(
      screen.getByText('3 retiradas por família · ciclo anual', { exact: false }),
    ).toBeInTheDocument()
  })

  it('não mostra indicadores da rede', async () => {
    mockServices()
    renderDashboard()

    await screen.findByText('Retiradas no período')
    expect(screen.queryByText('Crescimento da rede')).not.toBeInTheDocument()
    expect(screen.queryByText('Pontos por cidade')).not.toBeInTheDocument()
    expect(screen.queryByText('Distribuição por tipo')).not.toBeInTheDocument()
  })

  it('avisa quando o gestor não está vinculado a um ponto', () => {
    renderDashboard(null)

    expect(screen.getByText('Seu usuário não está vinculado a um ponto')).toBeInTheDocument()
  })

  it('busca o mês atual completo por padrão e refaz a busca ao mudar o período', async () => {
    const getDashboard = mockServices()
    renderDashboard()

    await screen.findByText('Retiradas no período')
    const { from, to } = currentMonthPeriod()
    const midMonth = `${from.slice(0, 8)}15`
    expect(getDashboard.mock.calls[0][1]).toEqual({ from, to })

    const endInput = document.getElementById('dashboard-to') as HTMLInputElement
    await userEvent.clear(endInput)
    await userEvent.type(endInput, isoToBrazilian(midMonth))

    await waitFor(() =>
      expect(getDashboard).toHaveBeenLastCalledWith(
        'point-1',
        { from, to: midMonth },
        expect.anything(),
      ),
    )
  })

  it('avisa quando a data inicial é maior que a final', async () => {
    mockServices()
    renderDashboard()
    await screen.findByText('Retiradas no período')

    const startInput = document.getElementById('dashboard-from') as HTMLInputElement
    await userEvent.clear(startInput)
    await userEvent.type(startInput, '30/12/2099')

    expect(await screen.findByRole('alert')).toHaveTextContent(/inicial não pode ser maior/i)
  })
})
