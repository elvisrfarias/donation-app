import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { collectionPointService } from '@/models/services'
import type { CollectionPoint } from '@/models/types'
import { StockEditor } from './StockEditor'

const point = {
  id: 'point-1',
  name: 'Casa Solidária',
  type: 'Association',
  address: { city: 'Concórdia', state: 'SC' },
  stock: [{ category: 'Food', quantity: 12 }],
  stockSettings: [
    { category: 'Food', goal: 50, alertThreshold: 10, isTracked: true },
    { category: 'Toys', goal: 0, alertThreshold: 5, isTracked: false },
  ],
} as CollectionPoint

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderEditor(canEdit = true) {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <StockEditor point={point} canEdit={canEdit} />
    </QueryClientProvider>,
  )
}

describe('StockEditor', () => {
  it('mostra o ponto e não permite salvar sem alterações', () => {
    renderEditor()

    expect(screen.getByText('Casa Solidária')).toBeInTheDocument()
    expect(screen.getByText('Nenhuma alteração pendente')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /salvar estoque/i })).toBeDisabled()
  })

  it('conta as categorias alteradas e permite descartar', async () => {
    renderEditor()

    await userEvent.click(screen.getByRole('button', { name: 'Aumentar Cestas básicas' }))

    expect(screen.getByText('1 categoria alterada')).toBeInTheDocument()
    expect(screen.getByLabelText('Quantidade de Cestas básicas')).toHaveValue(13)
    expect(screen.getByText('era 12')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: /descartar/i }))

    expect(screen.getByLabelText('Quantidade de Cestas básicas')).toHaveValue(12)
    expect(screen.getByText('Nenhuma alteração pendente')).toBeInTheDocument()
  })

  it('envia todas as categorias ao salvar', async () => {
    const updateStock = vi
      .spyOn(collectionPointService, 'updateStock')
      .mockResolvedValue({ ...point, stock: [{ category: 'Food', quantity: 13 }] })
    renderEditor()

    await userEvent.click(screen.getByRole('button', { name: 'Aumentar Cestas básicas' }))
    await userEvent.click(screen.getByRole('button', { name: /salvar estoque/i }))

    await waitFor(() => expect(updateStock).toHaveBeenCalledTimes(1))
    const [pointId, items] = updateStock.mock.calls[0]
    expect(pointId).toBe('point-1')
    expect(items).toHaveLength(11)
    expect(items).toContainEqual({ category: 'Food', quantity: 13 })
  })

  it('bloqueia a edição para quem não pode editar', () => {
    renderEditor(false)

    expect(screen.getByLabelText('Quantidade de Cestas básicas')).toBeDisabled()
    expect(screen.queryByRole('button', { name: /salvar estoque/i })).not.toBeInTheDocument()
  })

  it('mostra o progresso da meta na categoria que tem meta', () => {
    renderEditor()

    expect(screen.getByText('12 / 50')).toBeInTheDocument()
    expect(
      screen.getByRole('progressbar', { name: 'Progresso da meta de Cestas básicas' }),
    ).toBeInTheDocument()
  })

  it('esconde a categoria que não é acompanhada e está zerada', () => {
    renderEditor()

    expect(screen.queryByLabelText('Quantidade de Brinquedos')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Quantidade de Roupas')).toBeInTheDocument()
  })

  it('esconde a categoria não acompanhada mesmo com estoque e avisa que ela está oculta', () => {
    const pointWithHiddenStock = {
      ...point,
      stock: [
        { category: 'Food', quantity: 12 },
        { category: 'Toys', quantity: 7 },
      ],
    } as CollectionPoint
    const client = new QueryClient()
    render(
      <QueryClientProvider client={client}>
        <StockEditor point={pointWithHiddenStock} canEdit />
      </QueryClientProvider>,
    )

    expect(screen.queryByLabelText('Quantidade de Brinquedos')).not.toBeInTheDocument()
    expect(screen.getByText(/Brinquedos\) ficam ocultas/)).toBeInTheDocument()
  })

  it('deixa claro no resumo que os números contam categorias, não itens', () => {
    renderEditor()

    expect(screen.getByText('Categorias com estoque baixo')).toBeInTheDocument()
    expect(screen.getByText('Categorias em falta')).toBeInTheDocument()
    expect(screen.getByText('Unidades em estoque')).toBeInTheDocument()
    expect(screen.getByText('2 de 3', { selector: 'p' })).toBeInTheDocument()
    expect(screen.getByText('0 de 3', { selector: 'p' })).toBeInTheDocument()
  })

  it('avisa quais categorias precisam de reposição, sem incluir as não acompanhadas', () => {
    renderEditor()

    const banner = screen.getByText('Reposição necessária').parentElement as HTMLElement
    expect(banner).toHaveTextContent('Roupas')
    expect(banner).not.toHaveTextContent('Brinquedos')
    expect(banner).not.toHaveTextContent('Cestas básicas')
  })
})
