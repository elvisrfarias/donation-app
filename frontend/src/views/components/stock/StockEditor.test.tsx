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
  basketTemplates: [],
  stockItems: [
    {
      id: 'item-food',
      name: 'Arroz 1kg',
      unit: 'kg',
      category: 'Food',
      quantity: 12,
      goal: 50,
      alertThreshold: 10,
      isTracked: true,
      status: 'ok',
    },
    {
      id: 'item-clothing',
      name: 'Camiseta',
      unit: 'un',
      category: 'Clothing',
      quantity: 3,
      goal: 0,
      alertThreshold: 5,
      isTracked: true,
      status: 'low',
    },
  ],
} as unknown as CollectionPoint

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
  it('mostra o ponto de coleta', () => {
    renderEditor()

    expect(screen.getByText('Casa Solidária')).toBeInTheDocument()
  })

  it('ajusta a quantidade na hora e salva sozinho, sem precisar de um botão salvar', async () => {
    const updateStockQuantities = vi
      .spyOn(collectionPointService, 'updateStockQuantities')
      .mockResolvedValue({
        ...point,
        stockItems: [{ ...point.stockItems[0], quantity: 13 }, point.stockItems[1]],
      })
    renderEditor()

    expect(screen.queryByRole('button', { name: /salvar estoque/i })).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Aumentar Arroz 1kg' }))

    expect(screen.getByLabelText('Quantidade de Arroz 1kg')).toHaveValue(13)
    await waitFor(() => expect(updateStockQuantities).toHaveBeenCalledTimes(1))
    expect(updateStockQuantities).toHaveBeenCalledWith('point-1', {
      items: [{ stockItemId: 'item-food', quantity: 13 }],
    })
    await waitFor(() => expect(screen.getByText('Tudo salvo')).toBeInTheDocument())
  })

  it('bloqueia a edição para quem não pode editar', () => {
    renderEditor(false)

    expect(screen.getByLabelText('Quantidade de Arroz 1kg')).toBeDisabled()
    expect(screen.queryByRole('button', { name: 'Adicionar item' })).not.toBeInTheDocument()
  })

  it('mostra o progresso da meta no item que tem meta', () => {
    renderEditor()

    expect(screen.getByText('meta 50')).toBeInTheDocument()
    expect(
      screen.getByRole('progressbar', { name: 'Progresso da meta de Arroz 1kg' }),
    ).toBeInTheDocument()
  })

  it('agrupa os itens em seções por categoria', () => {
    renderEditor()

    expect(screen.getByText('Alimentos')).toBeInTheDocument()
    expect(screen.getByText('Roupas')).toBeInTheDocument()
  })

  it('filtra os itens pela busca e mostra a contagem de resultados', async () => {
    renderEditor()

    await userEvent.type(screen.getByPlaceholderText('Buscar item por nome…'), 'camiseta')

    expect(screen.getByText('1 resultado')).toBeInTheDocument()
    expect(screen.queryByLabelText('Quantidade de Arroz 1kg')).not.toBeInTheDocument()
    expect(screen.getByLabelText('Quantidade de Camiseta')).toBeInTheDocument()
  })

  it('mostra o resumo com a contagem de itens do catálogo', () => {
    renderEditor()

    expect(screen.getByText('Itens no catálogo')).toBeInTheDocument()
    expect(screen.getByText('Itens em falta')).toBeInTheDocument()
    expect(screen.getByText('Itens com estoque baixo')).toBeInTheDocument()
  })

  it('abre o formulário de edição preenchido ao clicar em editar', async () => {
    renderEditor()

    await userEvent.click(screen.getByRole('button', { name: 'Editar Arroz 1kg' }))

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByLabelText('Nome')).toHaveValue('Arroz 1kg')
  })

  it('confirma antes de remover um item e chama o serviço', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    const removeStockItem = vi
      .spyOn(collectionPointService, 'removeStockItem')
      .mockResolvedValue(undefined)
    renderEditor()

    await userEvent.click(screen.getByRole('button', { name: 'Remover Camiseta' }))

    expect(window.confirm).toHaveBeenCalled()
    await waitFor(() => expect(removeStockItem).toHaveBeenCalledWith('point-1', 'item-clothing'))
  })
})
