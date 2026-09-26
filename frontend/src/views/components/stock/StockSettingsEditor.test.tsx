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
  stock: [],
  stockSettings: [{ category: 'Food', goal: 50, alertThreshold: 10, isTracked: true }],
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

    expect(screen.getByLabelText('Meta de Cestas básicas')).toHaveValue(50)
    expect(screen.getByLabelText('Aviso de Cestas básicas')).toHaveValue(10)
    expect(screen.getByRole('button', { name: /salvar configurações/i })).toBeDisabled()
  })

  it('bloqueia o salvamento quando o aviso é maior que a meta', async () => {
    renderEditor()

    const alert = screen.getByLabelText('Aviso de Cestas básicas')
    await userEvent.clear(alert)
    await userEvent.type(alert, '60')

    expect(screen.getByText('O aviso não pode ser maior que a meta.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /salvar configurações/i })).toBeDisabled()
  })

  it('envia todas as categorias ao salvar, incluindo a alteração', async () => {
    const updateStockSettings = vi
      .spyOn(collectionPointService, 'updateStockSettings')
      .mockResolvedValue(point)
    renderEditor()

    const goal = screen.getByLabelText('Meta de Cestas básicas')
    await userEvent.clear(goal)
    await userEvent.type(goal, '80')
    await userEvent.click(screen.getByRole('button', { name: /salvar configurações/i }))

    await waitFor(() => expect(updateStockSettings).toHaveBeenCalledTimes(1))
    const [pointId, input] = updateStockSettings.mock.calls[0]
    expect(pointId).toBe('point-1')
    expect(input.items).toHaveLength(11)
    expect(input.items).toContainEqual({
      category: 'Food',
      goal: 80,
      alertThreshold: 10,
      isTracked: true,
    })
    expect(input.cycleType).toBe('Monthly')
    expect(input.maxWithdrawalsPerCycle).toBe(1)
  })

  it('desabilita meta e aviso de uma categoria que deixou de ser acompanhada', async () => {
    renderEditor()

    await userEvent.click(screen.getByRole('checkbox', { name: 'Acompanhar Cestas básicas' }))

    expect(screen.getByLabelText('Meta de Cestas básicas')).toBeDisabled()
    expect(screen.getByLabelText('Aviso de Cestas básicas')).toBeDisabled()
  })

  it('restaura os padrões e permite descartar a restauração', async () => {
    renderEditor()

    await userEvent.click(screen.getByRole('button', { name: /restaurar padrões/i }))
    expect(screen.getByLabelText('Meta de Cestas básicas')).toHaveValue(0)
    expect(screen.getByLabelText('Aviso de Cestas básicas')).toHaveValue(5)

    await userEvent.click(screen.getByRole('button', { name: /^descartar$/i }))
    expect(screen.getByLabelText('Meta de Cestas básicas')).toHaveValue(50)
  })

  it('bloqueia a edição para quem não pode editar', () => {
    renderEditor(false)

    expect(screen.getByLabelText('Meta de Cestas básicas')).toBeDisabled()
    expect(screen.queryByRole('button', { name: /salvar configurações/i })).not.toBeInTheDocument()
  })

  it('mostra a regra de retirada do ponto', () => {
    renderEditor()

    expect(screen.getByLabelText('Máximo por família em cada ciclo')).toHaveValue(1)
    expect(screen.getByRole('combobox', { name: /ciclo/i })).toHaveTextContent('Mensal')
  })

  it('salva a regra de retirada junto com as configurações', async () => {
    const updateStockSettings = vi
      .spyOn(collectionPointService, 'updateStockSettings')
      .mockResolvedValue(point)
    renderEditor()

    const max = screen.getByLabelText('Máximo por família em cada ciclo')
    await userEvent.clear(max)
    await userEvent.type(max, '3')
    expect(screen.getByText('regra de retirada alterada')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: /salvar configurações/i }))

    await waitFor(() => expect(updateStockSettings).toHaveBeenCalledTimes(1))
    expect(updateStockSettings.mock.calls[0][1].maxWithdrawalsPerCycle).toBe(3)
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
