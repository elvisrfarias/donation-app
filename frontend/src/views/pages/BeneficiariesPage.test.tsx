import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { beneficiaryService, collectionPointService } from '@/models/services'
import type { Beneficiary, CollectionPoint } from '@/models/types'
import { useAuthStore } from '@/stores/auth'
import { BeneficiariesPage } from './BeneficiariesPage'

const pointA = {
  id: 'point-a',
  name: 'Casa Solidária',
  address: { city: 'Concórdia', state: 'SC' },
} as unknown as CollectionPoint

const pointB = {
  id: 'point-b',
  name: 'Paróquia São José',
  address: { city: 'Joaçaba', state: 'SC' },
} as unknown as CollectionPoint

const maria: Beneficiary = {
  id: 'ben-maria',
  name: 'Maria Souza',
  cpf: '111.444.777-35',
  email: 'maria@example.com',
  phones: [],
  linkedPointIds: ['point-a'],
  isActive: true,
  kinships: [],
}

const joao: Beneficiary = {
  id: 'ben-joao',
  name: 'João Lima',
  cpf: '222.333.444-55',
  email: 'joao@example.com',
  phones: [],
  linkedPointIds: ['point-b'],
  isActive: true,
  kinships: [],
}

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <BeneficiariesPage />
    </QueryClientProvider>,
  )
}

describe('BeneficiariesPage — filtro por ponto do admin', () => {
  beforeEach(() => {
    vi.spyOn(collectionPointService, 'listPublic').mockResolvedValue([pointA, pointB])
    vi.spyOn(beneficiaryService, 'list').mockResolvedValue([maria, joao])
  })

  it('admin vê o filtro de ponto e a coluna "Ponto(s)", listando todos por padrão', async () => {
    useAuthStore.setState({
      user: {
        userId: 'admin-1',
        email: 'admin@x.com',
        fullName: 'Administrador',
        roles: ['Admin'],
        collectionPointId: null,
      },
      status: 'authenticated',
    })
    renderPage()

    await screen.findByText('Maria Souza')
    expect(screen.getByText('João Lima')).toBeInTheDocument()
    expect(screen.getByLabelText('Ponto')).toBeInTheDocument()
    expect(screen.getByText('Casa Solidária')).toBeInTheDocument()
  })

  it('filtra a lista ao escolher um ponto, sem esconder a opção de ver todos de novo', async () => {
    useAuthStore.setState({
      user: {
        userId: 'admin-1',
        email: 'admin@x.com',
        fullName: 'Administrador',
        roles: ['Admin'],
        collectionPointId: null,
      },
      status: 'authenticated',
    })
    renderPage()

    await screen.findByText('Maria Souza')
    await userEvent.click(screen.getByLabelText('Ponto'))
    await userEvent.click(await screen.findByRole('option', { name: 'Casa Solidária' }))

    await waitFor(() => expect(screen.queryByText('João Lima')).not.toBeInTheDocument())
    expect(screen.getByText('Maria Souza')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Ver todos' }))
    await waitFor(() => expect(screen.getByText('João Lima')).toBeInTheDocument())
  })

  it('gestor de ponto não vê o filtro nem a coluna "Ponto(s)"', async () => {
    useAuthStore.setState({
      user: {
        userId: 'manager-1',
        email: 'gestor@x.com',
        fullName: 'Gestor',
        roles: ['PointManager'],
        collectionPointId: 'point-a',
      },
      status: 'authenticated',
    })
    renderPage()

    await screen.findByText('Maria Souza')
    expect(screen.queryByLabelText('Ponto')).not.toBeInTheDocument()
    expect(screen.queryByText('Ponto(s)')).not.toBeInTheDocument()
  })
})
