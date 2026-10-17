import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { collectionPointService } from '@/models/services'
import type { CollectionPoint } from '@/models/types'
import { useAuthStore } from '@/stores/auth'
import { CollectionPointsPage } from './CollectionPointsPage'

const ownPoint = {
  id: 'point-a',
  name: 'Casa Solidária',
  type: 'Church',
  isActive: true,
  categories: [],
  needs: [],
  address: { city: 'Concórdia', state: 'SC' },
  contact: { responsibleName: 'Maria', email: 'maria@dooa.com.br', phone: '(49) 3442-1234' },
  averageRating: 0,
  reviewCount: 0,
  beneficiariesServed: 0,
} as unknown as CollectionPoint

beforeEach(() => {
  useAuthStore.setState({
    user: {
      userId: 'manager-1',
      email: 'gestor@dooa.com.br',
      fullName: 'Gestor',
      roles: ['PointManager'],
      collectionPointId: 'point-a',
    },
  })
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <CollectionPointsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('CollectionPointsPage para o gestor de ponto', () => {
  it('lista só o próprio ponto, com a ação de editar e sem cadastrar ou remover', async () => {
    const getById = vi.spyOn(collectionPointService, 'getById').mockResolvedValue(ownPoint)
    const listForManagement = vi.spyOn(collectionPointService, 'listForManagement')
    renderPage()

    expect((await screen.findAllByText('Casa Solidária')).length).toBeGreaterThan(0)
    expect(getById).toHaveBeenCalledWith('point-a', expect.anything())
    expect(listForManagement).not.toHaveBeenCalled()
    expect(screen.getAllByRole('button', { name: 'Editar' }).length).toBeGreaterThan(0)
    expect(screen.queryByRole('button', { name: 'Remover' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Cadastrar ponto/ })).not.toBeInTheDocument()
  })
})
