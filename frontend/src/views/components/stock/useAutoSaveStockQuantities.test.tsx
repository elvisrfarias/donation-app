import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, cleanup, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { collectionPointService } from '@/models/services'
import type { StockItem } from '@/models/types'
import { useAutoSaveStockQuantities } from './useAutoSaveStockQuantities'

const items: StockItem[] = [
  {
    id: 'item-food',
    name: 'Arroz 1kg',
    unit: 'kg',
    category: 'Food',
    quantity: 12,
    goal: 0,
    alertThreshold: 5,
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
    status: 'ok',
  },
]

function wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } })
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>
}

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  vi.useRealTimers()
})

describe('useAutoSaveStockQuantities', () => {
  it('mostra a mudança na hora e salva sozinho depois de uma pausa', async () => {
    vi.useFakeTimers()
    const updateStockQuantities = vi
      .spyOn(collectionPointService, 'updateStockQuantities')
      .mockResolvedValue({ id: 'point-1', stockItems: items } as never)

    const { result } = renderHook(() => useAutoSaveStockQuantities('point-1', items), { wrapper })

    act(() => result.current.setQuantity('item-food', 13))
    expect(result.current.quantityFor(items[0])).toBe(13)
    expect(updateStockQuantities).not.toHaveBeenCalled()

    await act(async () => {
      await vi.advanceTimersByTimeAsync(500)
    })

    expect(updateStockQuantities).toHaveBeenCalledTimes(1)
    expect(updateStockQuantities).toHaveBeenCalledWith('point-1', {
      items: [{ stockItemId: 'item-food', quantity: 13 }],
    })
  })

  it('junta várias mudanças rápidas num único envio', async () => {
    vi.useFakeTimers()
    const updateStockQuantities = vi
      .spyOn(collectionPointService, 'updateStockQuantities')
      .mockResolvedValue({ id: 'point-1', stockItems: items } as never)

    const { result } = renderHook(() => useAutoSaveStockQuantities('point-1', items), { wrapper })

    act(() => result.current.setQuantity('item-food', 13))
    act(() => result.current.setQuantity('item-clothing', 4))

    await act(async () => {
      await vi.advanceTimersByTimeAsync(500)
    })

    expect(updateStockQuantities).toHaveBeenCalledTimes(1)
    expect(updateStockQuantities.mock.calls[0][1].items).toEqual(
      expect.arrayContaining([
        { stockItemId: 'item-food', quantity: 13 },
        { stockItemId: 'item-clothing', quantity: 4 },
      ]),
    )
  })

  it('salva na hora quando saveNow é chamado, sem esperar a pausa', async () => {
    const updateStockQuantities = vi
      .spyOn(collectionPointService, 'updateStockQuantities')
      .mockResolvedValue({ id: 'point-1', stockItems: items } as never)

    const { result } = renderHook(() => useAutoSaveStockQuantities('point-1', items), { wrapper })

    act(() => result.current.setQuantity('item-food', 20))
    act(() => result.current.saveNow())

    await waitFor(() => expect(updateStockQuantities).toHaveBeenCalledTimes(1))
  })

  it('mantém a mudança pendente e mostra erro quando o envio falha', async () => {
    const updateStockQuantities = vi
      .spyOn(collectionPointService, 'updateStockQuantities')
      .mockRejectedValue(new Error('Falha de rede'))

    const { result } = renderHook(() => useAutoSaveStockQuantities('point-1', items), { wrapper })

    act(() => result.current.setQuantity('item-food', 20))
    act(() => result.current.saveNow())

    await waitFor(() => expect(result.current.status).toBe('error'))
    expect(result.current.errorMessage).toBe('Falha de rede')
    expect(result.current.quantityFor(items[0])).toBe(20)
    expect(updateStockQuantities).toHaveBeenCalledTimes(1)
  })

  it('não fica tentando de novo sozinho depois de uma falha, só quando o usuário pedir', async () => {
    const updateStockQuantities = vi
      .spyOn(collectionPointService, 'updateStockQuantities')
      .mockRejectedValue(new Error('Falha de rede'))

    const { result } = renderHook(() => useAutoSaveStockQuantities('point-1', items), { wrapper })

    act(() => result.current.setQuantity('item-food', 20))
    act(() => result.current.saveNow())
    await waitFor(() => expect(result.current.status).toBe('error'))

    await new Promise((resolve) => setTimeout(resolve, 200))
    expect(updateStockQuantities).toHaveBeenCalledTimes(1)

    updateStockQuantities.mockResolvedValue({ id: 'point-1', stockItems: items } as never)
    act(() => result.current.saveNow())

    await waitFor(() => expect(result.current.status).toBe('saved'))
    expect(updateStockQuantities).toHaveBeenCalledTimes(2)
    expect(updateStockQuantities).toHaveBeenLastCalledWith('point-1', {
      items: [{ stockItemId: 'item-food', quantity: 20 }],
    })
  })
})
