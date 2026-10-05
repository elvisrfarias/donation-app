import { describe, expect, it } from 'vitest'
import type { BasketItemGap, StockItem } from '@/models/types'
import {
  basketsPossibleForItem,
  bottleneckItemIds,
  describeNextBasketGap,
  insufficientItemsForQuantity,
  isBasketFeasibleForQuantity,
  joinNaturally,
  previewCompleteBaskets,
  unitsByItemId,
  type RecipeDraftItem,
} from './basketDraft'

function gap(overrides: Partial<BasketItemGap>): BasketItemGap {
  return {
    itemId: 'item-1',
    itemName: 'Arroz',
    requiredPerBasket: 1,
    currentQuantity: 0,
    neededForNextBasket: 0,
    ...overrides,
  }
}

describe('joinNaturally', () => {
  it('devolve a única parte sem conectivo', () => {
    expect(joinNaturally(['Arroz'])).toBe('Arroz')
  })

  it('une duas partes com "e"', () => {
    expect(joinNaturally(['Arroz', 'Açúcar'])).toBe('Arroz e Açúcar')
  })

  it('une três ou mais partes com vírgula e "e" antes da última', () => {
    expect(joinNaturally(['Arroz', 'Açúcar', 'Feijão'])).toBe('Arroz, Açúcar e Feijão')
  })

  it('devolve string vazia para lista vazia', () => {
    expect(joinNaturally([])).toBe('')
  })
})

describe('basketsPossibleForItem', () => {
  it('calcula quantas cestas o item permite isoladamente', () => {
    expect(basketsPossibleForItem({ currentQuantity: 10, requiredPerBasket: 3 })).toBe(3)
  })

  it('trata quantidade requerida zero como sem limite', () => {
    expect(basketsPossibleForItem({ currentQuantity: 0, requiredPerBasket: 0 })).toBe(
      Number.POSITIVE_INFINITY,
    )
  })
})

describe('bottleneckItemIds', () => {
  it('marca como gargalo apenas o(s) item(ns) que batem o total de cestas completas', () => {
    const missing = [
      gap({ itemId: 'rice', currentQuantity: 10, requiredPerBasket: 2 }),
      gap({ itemId: 'sugar', currentQuantity: 3, requiredPerBasket: 1 }),
    ]

    expect(bottleneckItemIds(missing, 3)).toEqual(new Set(['sugar']))
  })

  it('marca mais de um item quando ambos empatam no limite', () => {
    const missing = [
      gap({ itemId: 'rice', currentQuantity: 6, requiredPerBasket: 2 }),
      gap({ itemId: 'sugar', currentQuantity: 3, requiredPerBasket: 1 }),
    ]

    expect(bottleneckItemIds(missing, 3)).toEqual(new Set(['rice', 'sugar']))
  })
})

describe('describeNextBasketGap', () => {
  it('descreve o que falta para montar mais uma cesta, com unidade', () => {
    const missing = [
      gap({ itemId: 'rice', itemName: 'Arroz', neededForNextBasket: 2 }),
      gap({ itemId: 'sugar', itemName: 'Açúcar', neededForNextBasket: 1 }),
    ]
    const units = unitsByItemId([
      { id: 'rice', unit: 'kg' } as StockItem,
      { id: 'sugar', unit: 'pacote' } as StockItem,
    ])

    expect(describeNextBasketGap(missing, units)).toBe(
      'Faltam 2 kg de Arroz e 1 pacote de Açúcar para montar mais 1 cesta.',
    )
  })

  it('omite a unidade "un"', () => {
    const missing = [gap({ itemId: 'shirt', itemName: 'Camiseta', neededForNextBasket: 3 })]
    const units = unitsByItemId([{ id: 'shirt', unit: 'un' } as StockItem])

    expect(describeNextBasketGap(missing, units)).toBe(
      'Faltam 3 de Camiseta para montar mais 1 cesta.',
    )
  })

  it('devolve null quando nada falta para a próxima cesta', () => {
    const missing = [gap({ neededForNextBasket: 0 })]

    expect(describeNextBasketGap(missing, {})).toBeNull()
  })
})

describe('insufficientItemsForQuantity / isBasketFeasibleForQuantity', () => {
  it('não acusa nada quando o estoque cobre a quantidade pedida', () => {
    const missing = [gap({ currentQuantity: 10, requiredPerBasket: 2 })]

    expect(isBasketFeasibleForQuantity(missing, 3)).toBe(true)
    expect(insufficientItemsForQuantity(missing, 3)).toEqual([])
  })

  it('acusa todos os itens insuficientes, não só o primeiro', () => {
    const missing = [
      gap({ itemId: 'rice', itemName: 'Arroz', currentQuantity: 2, requiredPerBasket: 2 }),
      gap({ itemId: 'sugar', itemName: 'Açúcar', currentQuantity: 1, requiredPerBasket: 2 }),
    ]

    expect(isBasketFeasibleForQuantity(missing, 2)).toBe(false)
    expect(insufficientItemsForQuantity(missing, 2)).toEqual([
      { itemId: 'rice', itemName: 'Arroz', unit: undefined, missingAmount: 2 },
      { itemId: 'sugar', itemName: 'Açúcar', unit: undefined, missingAmount: 3 },
    ])
  })

  it('quantidade zero ou negativa nunca é viável', () => {
    const missing = [gap({ currentQuantity: 10, requiredPerBasket: 1 })]

    expect(isBasketFeasibleForQuantity(missing, 0)).toBe(false)
  })
})

describe('previewCompleteBaskets', () => {
  function stockItemsMap(items: Partial<StockItem>[]): Map<string, StockItem> {
    return new Map(items.map((item) => [item.id as string, item as StockItem]))
  }

  it('devolve zero para uma cesta sem itens', () => {
    expect(previewCompleteBaskets([], new Map())).toBe(0)
  })

  it('calcula o mínimo entre os itens do rascunho', () => {
    const items: RecipeDraftItem[] = [
      { stockItemId: 'rice', itemName: 'Arroz', unit: 'kg', quantity: 2 },
      { stockItemId: 'sugar', itemName: 'Açúcar', unit: 'pacote', quantity: 1 },
    ]
    const stockItems = stockItemsMap([
      { id: 'rice', quantity: 10 },
      { id: 'sugar', quantity: 3 },
    ])

    expect(previewCompleteBaskets(items, stockItems)).toBe(3)
  })

  it('devolve zero se algum item do rascunho ainda não tem quantidade válida', () => {
    const items: RecipeDraftItem[] = [
      { stockItemId: 'rice', itemName: 'Arroz', unit: 'kg', quantity: 0 },
    ]
    const stockItems = stockItemsMap([{ id: 'rice', quantity: 10 }])

    expect(previewCompleteBaskets(items, stockItems)).toBe(0)
  })

  it('devolve zero se um item do rascunho não existir mais no catálogo', () => {
    const items: RecipeDraftItem[] = [
      { stockItemId: 'ghost', itemName: 'Fantasma', unit: 'un', quantity: 1 },
    ]

    expect(previewCompleteBaskets(items, new Map())).toBe(0)
  })
})
