import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { sponsorService } from '@/models/services'
import type { Sponsor } from '@/models/types'
import { SponsorsSection } from './SponsorsSection'

function sponsor(overrides: Partial<Sponsor>): Sponsor {
  return {
    id: crypto.randomUUID(),
    name: 'Instituição',
    tier: 'Silver',
    websiteUrl: null,
    logoUrl: 'https://arquivos.dooa.com.br/logo.png',
    isActive: true,
    ...overrides,
  }
}

class InertIntersectionObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}

beforeEach(() => {
  vi.stubGlobal('IntersectionObserver', InertIntersectionObserver)
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})

function renderSection() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <SponsorsSection />
    </QueryClientProvider>,
  )
}

describe('SponsorsSection', () => {
  it('agrupa os patrocinadores do Diamante à Prata', async () => {
    vi.spyOn(sponsorService, 'listPublic').mockResolvedValue([
      sponsor({ name: 'Mercado Bom Preço', tier: 'Silver' }),
      sponsor({ name: 'Paróquia São José', tier: 'Diamond' }),
      sponsor({ name: 'Associação Vida', tier: 'Gold' }),
    ])
    renderSection()

    const headings = await screen.findAllByRole('heading', { level: 3 })
    expect(headings.map((heading) => heading.textContent)).toEqual(['Diamante', 'Ouro', 'Prata'])
    const diamondGroup = headings[0].parentElement as HTMLElement
    expect(within(diamondGroup).getByText('Paróquia São José')).toBeInTheDocument()
    expect(screen.getByText('Quer ver sua instituição aqui?')).toBeInTheDocument()
  })

  it('não mostra categorias vazias', async () => {
    vi.spyOn(sponsorService, 'listPublic').mockResolvedValue([
      sponsor({ name: 'Paróquia São José', tier: 'Diamond' }),
    ])
    renderSection()

    expect(await screen.findByText('Diamante')).toBeInTheDocument()
    expect(screen.queryByText('Ouro')).not.toBeInTheDocument()
    expect(screen.queryByText('Prata')).not.toBeInTheDocument()
  })

  it('leva ao site do patrocinador numa nova aba, marcado como patrocinado', async () => {
    vi.spyOn(sponsorService, 'listPublic').mockResolvedValue([
      sponsor({ name: 'Paróquia São José', tier: 'Gold', websiteUrl: 'https://paroquia.org.br' }),
    ])
    renderSection()

    const link = (await screen.findByText('Paróquia São José')).closest('a')
    expect(link).toHaveAttribute('href', 'https://paroquia.org.br')
    expect(link).toHaveAttribute('target', '_blank')
    expect(link).toHaveAttribute('rel', 'sponsored noopener noreferrer')
  })

  it('sem patrocinadores, convida a instituição a ser a primeira', async () => {
    vi.spyOn(sponsorService, 'listPublic').mockResolvedValue([])
    renderSection()

    expect(
      await screen.findByText('Sua instituição pode ser a primeira a patrocinar o Dooa'),
    ).toBeInTheDocument()
    expect(screen.queryAllByRole('heading', { level: 3 })).toHaveLength(0)
  })
})
