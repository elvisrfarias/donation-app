import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/models/api/problemDetails'
import { collectionPointService } from '@/models/services'
import type { CollectionPoint } from '@/models/types'
import { PointPage } from './PointPage'

const casaSolidaria = {
  id: 'point-1',
  slug: 'casa-solidaria-concordia-sc',
  name: 'Casa Solidária',
  type: 'Church',
  description: 'Acolhimento e doação de alimentos.',
  openingHours: 'Seg a sex, 8h às 17h',
  needs: ['Leite em pó', 'Fraldas'],
  categories: [],
  stockItems: [],
  basketTemplates: [],
  address: {
    street: 'Rua das Flores',
    number: '100',
    complement: null,
    district: 'Centro',
    city: 'Concórdia',
    state: 'SC',
    zipCode: '89700-000',
    latitude: -27.23,
    longitude: -52.02,
  },
  contact: { responsibleName: 'Maria', email: 'maria@dooa.com.br', phone: '(49) 99876-5432' },
  isActive: true,
  imageUrl: 'https://arquivos.dooa.com.br/casa.png',
  averageRating: 4.5,
  reviewCount: 2,
  beneficiariesServed: 10,
} as unknown as CollectionPoint

beforeEach(() => {
  document.head.innerHTML =
    '<meta name="description" content="" /><meta name="robots" content="index, follow" />'
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function CurrentPath() {
  const location = useLocation()
  return <span data-testid="path">{location.pathname}</span>
}

function renderAt(path: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/pontos/:pointId/:slug?" element={<PointPage />} />
        </Routes>
        <CurrentPath />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('PointPage', () => {
  it('mostra os dados do ponto e ajusta título, descrição e canonical', async () => {
    vi.spyOn(collectionPointService, 'getPublic').mockResolvedValue(casaSolidaria)
    renderAt('/pontos/point-1/casa-solidaria-concordia-sc')

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Casa Solidária' }),
    ).toBeInTheDocument()
    expect(screen.getByText('Leite em pó')).toBeInTheDocument()
    expect(
      document.head.querySelector('meta[name="description"]')?.getAttribute('content'),
    ).toContain('Precisa agora: Leite em pó, Fraldas.')
    expect(
      screen.getByText('Rua das Flores, 100, Centro, Concórdia - SC, 89700-000'),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Como chegar' })).toHaveAttribute('target', '_blank')
    expect(document.title).toBe('Casa Solidária — ponto de doação em Concórdia/SC | Dooa')
    expect(document.head.querySelector('link[rel="canonical"]')).toHaveAttribute(
      'href',
      `${window.location.origin}/pontos/point-1/casa-solidaria-concordia-sc`,
    )
  })

  it('corrige o endereço quando o nome do ponto mudou', async () => {
    vi.spyOn(collectionPointService, 'getPublic').mockResolvedValue(casaSolidaria)
    renderAt('/pontos/point-1/nome-antigo')

    expect(
      await screen.findByRole('heading', { level: 1, name: 'Casa Solidária' }),
    ).toBeInTheDocument()
    expect(screen.getByTestId('path')).toHaveTextContent(
      '/pontos/point-1/casa-solidaria-concordia-sc',
    )
  })

  it('ponto inexistente mostra a página não encontrada, fora do índice', async () => {
    vi.spyOn(collectionPointService, 'getPublic').mockRejectedValue(
      new ApiError({ status: 404, title: 'Ponto de coleta não encontrado.' }),
    )
    renderAt('/pontos/nao-existe/qualquer')

    expect(
      await screen.findByText('A página que você procura não existe ou foi movida.'),
    ).toBeInTheDocument()
    expect(document.head.querySelector('meta[name="robots"]')).toHaveAttribute(
      'content',
      'noindex, nofollow',
    )
  })
})
