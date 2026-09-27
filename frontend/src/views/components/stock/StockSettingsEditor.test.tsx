import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { collectionPointService } from '@/models/services'
import type { CollectionPoint } from '@/models/types'
import { StockSettingsEditor } from './StockSettingsEditor'

const point = {
  id: 'point-1',
  name: 'Casa Solidária',
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
  ],
  withdrawalRule: { type: 'Monthly', maxWithdrawalsPerCycle: 1 },
} as unknown as CollectionPoint

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderEditor(canEdit = true) {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <StockSettingsEditor point={point} canEdit={canEdit} />
    </QueryClientProvider>,
  )
}

describe('StockSettingsEditor', () => {
  it('mostra a meta e o aviso já configurados e não permite salvar sem alterações', () => {
    renderEditor()

    expect(screen.getByLabelText('Meta de Arroz 1kg')).toHaveValue(50)
    expect(screen.getByLabelText('Aviso de Arroz 1kg')).toHaveValue(10)
    expect(screen.getByRole('button', { name: /salvar configurações/i })).toBeDisabled()
  })

  it('bloqueia o salvamento quando o aviso é maior que a meta', async () => {
    renderEditor()

    const alert = screen.getByLabelText('Aviso de Arroz 1kg')
    await userEvent.clear(alert)
    await userEvent.type(alert, '60')

    expect(screen.getByText('O aviso não pode ser maior que a meta.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /salvar configurações/i })).toBeDisabled()
  })

  it('envia só o item alterado ao salvar, sem mexer na regra de retirada', async () => {
    const updateStockItemSettings = vi
      .spyOn(collectionPointService, 'updateStockItemSettings')
      .mockResolvedValue(point)
    const updateWithdrawalRule = vi.spyOn(collectionPointService, 'updateWithdrawalRule')
    renderEditor()

    const goal = screen.getByLabelText('Meta de Arroz 1kg')
    await userEvent.clear(goal)
    await userEvent.type(goal, '80')
    await userEvent.click(screen.getByRole('button', { name: /salvar configurações/i }))

    await waitFor(() => expect(updateStockItemSettings).toHaveBeenCalledTimes(1))
    const [pointId, input] = updateStockItemSettings.mock.calls[0]
    expect(pointId).toBe('point-1')
    expect(input.items).toContainEqual({
      stockItemId: 'item-food',
      goal: 80,
      alertThreshold: 10,
      isTracked: true,
    })
    expect(updateWithdrawalRule).not.toHaveBeenCalled()
  })

  it('desabilita meta e aviso de um item que deixou de ser acompanhado', async () => {
    renderEditor()

    await userEvent.click(screen.getByRole('checkbox', { name: 'Acompanhar Arroz 1kg' }))

    expect(screen.getByLabelText('Meta de Arroz 1kg')).toBeDisabled()
    expect(screen.getByLabelText('Aviso de Arroz 1kg')).toBeDisabled()
  })

  it('bloqueia a edição para quem não pode editar', () => {
    renderEditor(false)

    expect(screen.getByLabelText('Meta de Arroz 1kg')).toBeDisabled()
    expect(screen.queryByRole('button', { name: /salvar configurações/i })).not.toBeInTheDocument()
  })

  it('mostra a regra de retirada do ponto', () => {
    renderEditor()

    expect(screen.getByLabelText('Máximo por família em cada ciclo')).toHaveValue(1)
    expect(screen.getByRole('combobox', { name: /ciclo/i })).toHaveTextContent('Mensal')
  })

  it('salva só a regra de retirada quando é a única coisa alterada', async () => {
    const updateWithdrawalRule = vi
      .spyOn(collectionPointService, 'updateWithdrawalRule')
      .mockResolvedValue(point)
    const updateStockItemSettings = vi.spyOn(collectionPointService, 'updateStockItemSettings')
    renderEditor()

    const max = screen.getByLabelText('Máximo por família em cada ciclo')
    await userEvent.clear(max)
    await userEvent.type(max, '3')
    expect(screen.getByText('regra de retirada alterada')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /salvar configurações/i }))

    await waitFor(() => expect(updateWithdrawalRule).toHaveBeenCalledTimes(1))
    expect(updateWithdrawalRule.mock.calls[0][1].maxWithdrawalsPerCycle).toBe(3)
    expect(updateStockItemSettings).not.toHaveBeenCalled()
  })

  it('bloqueia o salvamento quando o máximo por ciclo é zero', async () => {
    renderEditor()

    const max = screen.getByLabelText('Máximo por família em cada ciclo')
    await userEvent.clear(max)
    await userEvent.type(max, '0')

    expect(screen.getByText('O máximo por ciclo deve ser de pelo menos 1.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /salvar configurações/i })).toBeDisabled()
  })
})
