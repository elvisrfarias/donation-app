import { describe, expect, it } from 'vitest'
import type { CollectionPoint, StockItem } from '@/models/types'
import {
  availableQuantityForItem,
  availableWithdrawalItems,
  insufficientWithdrawalItems,
  type WithdrawalDraftItem,
} from './withdrawalItems'

function pointWith(stockItems: Partial<StockItem>[]): CollectionPoint {
  return {
    stockItems: stockItems.map((item, index) => ({
      id: `item-${index}`,
      name: `Item ${index}`,
      unit: 'un',
      categoryId: 'cat-food',
      categoryName: 'Alimentos',
      categoryIcon: 'apple',
      quantity: 0,
      goal: 0,
      alertThreshold: 5,
      isTracked: true,
      status: 'ok',
      ...item,
    })),
  } as CollectionPoint
}

describe('availableWithdrawalItems', () => {
  it('lista só itens acompanhados e com estoque positivo', () => {
    const point = pointWith([
      { id: 'rice', name: 'Arroz 1kg', quantity: 5, isTracked: true },
      { id: 'shirt', name: 'Camiseta', quantity: 3, isTracked: true },
      { id: 'shoes', name: 'Tênis', quantity: 2, isTracked: false },
    ])

    expect(availableWithdrawalItems(point).map((item) => item.id)).toEqual(['rice', 'shirt'])
  })

  it('não lista um item acompanhado que esteja zerado no estoque', () => {
    const point = pointWith([{ id: 'rice', quantity: 0, isTracked: true }])

    expect(availableWithdrawalItems(point)).toEqual([])
  })

  it('não lista um item com estoque positivo se não é acompanhado', () => {
    const point = pointWith([{ id: 'pill', quantity: 10, isTracked: false }])

    expect(availableWithdrawalItems(point)).toEqual([])
  })

  it('sem nenhum item no catálogo, não lista nada', () => {
    expect(availableWithdrawalItems(pointWith([]))).toEqual([])
  })
})

describe('availableQuantityForItem', () => {
  it('na criação (reserva 0), é só o estoque atual', () => {
    expect(availableQuantityForItem({ quantity: 4 }, 0)).toBe(4)
  })

  it('na edição, soma de volta a reserva desta mesma retirada', () => {
    expect(availableQuantityForItem({ quantity: 1 }, 3)).toBe(4)
  })
})

describe('insufficientWithdrawalItems', () => {
  const stockItemsById = new Map<string, StockItem>([
    ['rice', { id: 'rice', name: 'Arroz', unit: 'kg', quantity: 2 } as StockItem],
  ])

  it('sem itens insuficientes quando a quantidade cabe no estoque', () => {
    const items: WithdrawalDraftItem[] = [{ stockItemId: 'rice', itemName: 'Arroz', unit: 'kg', quantity: 2 }]

    expect(insufficientWithdrawalItems(items, stockItemsById)).toEqual([])
  })

  it('aponta o item quando a quantidade pedida excede o estoque disponível', () => {
    const items: WithdrawalDraftItem[] = [{ stockItemId: 'rice', itemName: 'Arroz', unit: 'kg', quantity: 5 }]

    expect(insufficientWithdrawalItems(items, stockItemsById)).toEqual([
      { stockItemId: 'rice', itemName: 'Arroz', unit: 'kg', missingAmount: 3 },
    ])
  })

  it('na edição, considera a reserva desta mesma retirada antes de bloquear', () => {
    const items: WithdrawalDraftItem[] = [{ stockItemId: 'rice', itemName: 'Arroz', unit: 'kg', quantity: 5 }]

    expect(insufficientWithdrawalItems(items, stockItemsById, { rice: 3 })).toEqual([])
  })
})
