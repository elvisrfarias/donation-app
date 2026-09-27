import { describe, expect, it } from 'vitest'
import type { CollectionPoint } from '@/models/types'
import { availableWithdrawalCategories } from './withdrawalCategories'

function pointWith(
  stock: CollectionPoint['stock'],
  stockSettings: CollectionPoint['stockSettings'],
): CollectionPoint {
  return { stock, stockSettings } as CollectionPoint
}

describe('availableWithdrawalCategories', () => {
  it('lista só categorias acompanhadas e com estoque positivo', () => {
    const point = pointWith(
      [
        { category: 'Food', quantity: 5 },
        { category: 'Clothing', quantity: 3 },
        { category: 'Footwear', quantity: 2 },
      ],
      [
        { category: 'Food', goal: 0, alertThreshold: 5, isTracked: true },
        { category: 'Clothing', goal: 0, alertThreshold: 5, isTracked: true },
        { category: 'Footwear', goal: 0, alertThreshold: 5, isTracked: false },
      ],
    )

    expect(availableWithdrawalCategories(point)).toEqual(['Food', 'Clothing'])
  })

  it('não lista uma categoria acompanhada que esteja zerada no estoque', () => {
    const point = pointWith(
      [{ category: 'Food', quantity: 0 }],
      [{ category: 'Food', goal: 0, alertThreshold: 5, isTracked: true }],
    )

    expect(availableWithdrawalCategories(point)).toEqual([])
  })

  it('não lista uma categoria com estoque positivo se ela não é acompanhada', () => {
    const point = pointWith(
      [{ category: 'Medicine', quantity: 10 }],
      [{ category: 'Medicine', goal: 0, alertThreshold: 5, isTracked: false }],
    )

    expect(availableWithdrawalCategories(point)).toEqual([])
  })

  it('sem nenhuma configuração salva, usa o padrão (cestas básicas, roupas e calçados)', () => {
    const point = pointWith(
      [
        { category: 'Food', quantity: 5 },
        { category: 'Clothing', quantity: 5 },
        { category: 'Footwear', quantity: 5 },
        { category: 'Medicine', quantity: 5 },
      ],
      [],
    )

    expect(availableWithdrawalCategories(point)).toEqual(['Food', 'Clothing', 'Footwear'])
  })
})
