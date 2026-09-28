import { describe, expect, it } from 'vitest'
import type { Category, CollectionPoint, StockItem } from '@/models/types'
import { availableWithdrawalCategories } from './withdrawalCategories'

const food: Category = { id: 'cat-food', name: 'Alimentos', icon: 'apple' }
const clothing: Category = { id: 'cat-clothing', name: 'Roupas', icon: 'shirt' }
const footwear: Category = { id: 'cat-footwear', name: 'Calçados', icon: 'footprints' }
const medicine: Category = { id: 'cat-medicine', name: 'Medicamentos', icon: 'pill' }

function pointWith(categories: Category[], stockItems: Partial<StockItem>[]): CollectionPoint {
  return {
    categories,
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

describe('availableWithdrawalCategories', () => {
  it('lista só categorias acompanhadas e com estoque positivo', () => {
    const point = pointWith(
      [food, clothing, footwear],
      [
        { categoryId: food.id, quantity: 5, isTracked: true },
        { categoryId: clothing.id, quantity: 3, isTracked: true },
        { categoryId: footwear.id, quantity: 2, isTracked: false },
      ],
    )

    expect(availableWithdrawalCategories(point)).toEqual([food, clothing])
  })

  it('não lista uma categoria acompanhada que esteja zerada no estoque', () => {
    const point = pointWith([food], [{ categoryId: food.id, quantity: 0, isTracked: true }])

    expect(availableWithdrawalCategories(point)).toEqual([])
  })

  it('não lista uma categoria com estoque positivo se nenhum item dela é acompanhado', () => {
    const point = pointWith(
      [medicine],
      [{ categoryId: medicine.id, quantity: 10, isTracked: false }],
    )

    expect(availableWithdrawalCategories(point)).toEqual([])
  })

  it('basta um item acompanhado com estoque para liberar a categoria', () => {
    const point = pointWith(
      [food],
      [
        { categoryId: food.id, quantity: 0, isTracked: true },
        { categoryId: food.id, quantity: 4, isTracked: false },
        { categoryId: food.id, quantity: 2, isTracked: true },
      ],
    )

    expect(availableWithdrawalCategories(point)).toEqual([food])
  })

  it('sem nenhum item no catálogo, não lista nenhuma categoria', () => {
    const point = pointWith([food, clothing], [])

    expect(availableWithdrawalCategories(point)).toEqual([])
  })
})
