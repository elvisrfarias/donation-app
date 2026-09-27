import { describe, expect, it } from 'vitest'
import type { CollectionPoint, StockItem } from '@/models/types'
import { availableWithdrawalCategories } from './withdrawalCategories'

function pointWith(stockItems: Partial<StockItem>[]): CollectionPoint {
  return {
    stockItems: stockItems.map((item, index) => ({
      id: `item-${index}`,
      name: `Item ${index}`,
      unit: 'un',
      goal: 0,
      alertThreshold: 5,
      isTracked: true,
      status: 'ok',
      quantity: 0,
      category: 'Other',
      ...item,
    })),
  } as CollectionPoint
}

describe('availableWithdrawalCategories', () => {
  it('lista só categorias acompanhadas e com estoque positivo', () => {
    const point = pointWith([
      { category: 'Food', quantity: 5, isTracked: true },
      { category: 'Clothing', quantity: 3, isTracked: true },
      { category: 'Footwear', quantity: 2, isTracked: false },
    ])

    expect(availableWithdrawalCategories(point)).toEqual(['Food', 'Clothing'])
  })

  it('não lista uma categoria acompanhada que esteja zerada no estoque', () => {
    const point = pointWith([{ category: 'Food', quantity: 0, isTracked: true }])

    expect(availableWithdrawalCategories(point)).toEqual([])
  })

  it('não lista uma categoria com estoque positivo se nenhum item dela é acompanhado', () => {
    const point = pointWith([{ category: 'Medicine', quantity: 10, isTracked: false }])

    expect(availableWithdrawalCategories(point)).toEqual([])
  })

  it('basta um item acompanhado com estoque para liberar a categoria', () => {
    const point = pointWith([
      { category: 'Food', quantity: 0, isTracked: true },
      { category: 'Food', quantity: 4, isTracked: false },
      { category: 'Food', quantity: 2, isTracked: true },
    ])

    expect(availableWithdrawalCategories(point)).toEqual(['Food'])
  })

  it('sem nenhum item no catálogo, não lista nenhuma categoria', () => {
    const point = pointWith([])

    expect(availableWithdrawalCategories(point)).toEqual([])
  })
})
