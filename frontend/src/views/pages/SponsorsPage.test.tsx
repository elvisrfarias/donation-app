import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { sponsorService } from '@/models/services'
import type { Sponsor } from '@/models/types'
import { SponsorsPage } from './SponsorsPage'

const paroquia: Sponsor = {
  id: 'sponsor-1',
  name: 'Paróquia São José',
  tier: 'Gold',
  websiteUrl: 'https://paroquia.org.br',
  logoUrl: 'https://arquivos.dooa.com.br/logo.png',
  isActive: true,
}

beforeEach(() => {
  vi.spyOn(sponsorService, 'list').mockResolvedValue([paroquia])
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <SponsorsPage />
    </QueryClientProvider>,
  )
}

describe('SponsorsPage', () => {
  it('lista os patrocinadores com a categoria', async () => {
    renderPage()

    expect((await screen.findAllByText('Paróquia São José')).length).toBeGreaterThan(0)
    expect(screen.getAllByText('Ouro').length).toBeGreaterThan(0)
  })

  it('edita a categoria e tira do site sem reenviar a logo', async () => {
    const update = vi.spyOn(sponsorService, 'update').mockResolvedValue(paroquia)
    const uploadLogo = vi.spyOn(sponsorService, 'uploadLogo')
    renderPage()

    await userEvent.click((await screen.findAllByRole('button', { name: 'Editar' }))[0])
    await userEvent.click(screen.getByLabelText('Exibir na página pública'))
    await userEvent.click(screen.getByRole('button', { name: /Salvar alterações/ }))

    await waitFor(() =>
      expect(update).toHaveBeenCalledWith('sponsor-1', {
        name: 'Paróquia São José',
        tier: 'Gold',
        websiteUrl: 'https://paroquia.org.br',
        isActive: false,
        logoFileId: undefined,
      }),
    )
    expect(uploadLogo).not.toHaveBeenCalled()
  })

  it('recusa site sem https://', async () => {
    const update = vi.spyOn(sponsorService, 'update')
    renderPage()

    await userEvent.click((await screen.findAllByRole('button', { name: 'Editar' }))[0])
    const website = screen.getByLabelText(/^Site ou rede social/)
    await userEvent.clear(website)
    await userEvent.type(website, 'paroquia.org.br')
    await userEvent.click(screen.getByRole('button', { name: /Salvar alterações/ }))

    expect(
      await screen.findByText('Informe o site completo, começando com https://.'),
    ).toBeInTheDocument()
    expect(update).not.toHaveBeenCalled()
  })
})
