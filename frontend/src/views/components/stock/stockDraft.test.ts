import { describe, expect, it } from 'vitest'
import type { BasketTemplate, Category, CollectionPoint, StockItem } from '@/models/types'
import {
  DEFAULT_ALERT_THRESHOLD,
  MAX_QUANTITY,
  changedSettingIds,
  countByStatus,
  goalProgress,
  groupByCategory,
  isRuleChanged,
  itemsNeedingRestock,
  parseQuantity,
  ruleError,
  settingError,
  sumQuantities,
  sumQuantitiesByCategory,
  toSettingEntries,
  toSettingsDraft,
  totalCompleteBaskets,
  toWithdrawalRule,
} from './stockDraft'

const CATEGORY_FOOD: Category = { id: 'food', name: 'Alimentos', icon: 'apple' }
const CATEGORY_CLOTHING: Category = { id: 'clothing', name: 'Roupas', icon: 'shirt' }
const CATEGORY_FOOTWEAR: Category = { id: 'footwear', name: 'Calçados', icon: 'footprints' }
const CATEGORY_TOYS: Category = { id: 'toys', name: 'Brinquedos', icon: 'puzzle' }
const CATEGORY_BOOKS: Category = { id: 'books', name: 'Livros', icon: 'book-open' }

const categories: Category[] = [
  CATEGORY_FOOD,
  CATEGORY_CLOTHING,
  CATEGORY_FOOTWEAR,
  CATEGORY_TOYS,
  CATEGORY_BOOKS,
]

function stockItem(
  overrides: Partial<StockItem> & Pick<StockItem, 'id'> & { category: Category },
): StockItem {
  const { category, ...rest } = overrides
  return {
    name: 'Item',
    unit: 'un',
    categoryId: category.id,
    categoryName: category.name,
    categoryIcon: category.icon,
    quantity: 0,
    goal: 0,
    alertThreshold: DEFAULT_ALERT_THRESHOLD,
    isTracked: true,
    status: 'ok',
    ...rest,
  }
}

const items: StockItem[] = [
  stockItem({ id: 'food-1', category: CATEGORY_FOOD, name: 'Arroz', quantity: 12, goal: 50, status: 'ok' }),
  stockItem({ id: 'clothing-1', category: CATEGORY_CLOTHING, name: 'Camiseta', quantity: 3, status: 'low' }),
  stockItem({ id: 'footwear-1', category: CATEGORY_FOOTWEAR, name: 'Tênis', quantity: 0, status: 'out' }),
  stockItem({
    id: 'toys-1',
    category: CATEGORY_TOYS,
    name: 'Boneca',
    quantity: 40,
    isTracked: false,
    status: 'ok',
  }),
]

const pointWithRule = { withdrawalRule: { type: 'Weekly', maxWithdrawalsPerCycle: 2 } } as CollectionPoint

describe('stockDraft — quantidades', () => {
  it('interpreta o texto digitado como inteiro entre zero e o máximo', () => {
    expect(parseQuantity('')).toBe(0)
    expect(parseQuantity('abc')).toBe(0)
    expect(parseQuantity('-4')).toBe(0)
    expect(parseQuantity('7.9')).toBe(7)
    expect(parseQuantity(String(MAX_QUANTITY + 500))).toBe(MAX_QUANTITY)
  })

  it('agrupa os itens pelas categorias reais do ponto, ordenadas por nome, incluindo as sem item', () => {
    const groups = groupByCategory(items, categories)

    expect(groups.map((g) => g.category.name)).toEqual([
      'Alimentos',
      'Brinquedos',
      'Calçados',
      'Livros',
      'Roupas',
    ])
    const foodGroup = groups.find((g) => g.category.id === CATEGORY_FOOD.id)
    expect(foodGroup?.items).toEqual([items[0]])
    const booksGroup = groups.find((g) => g.category.id === CATEGORY_BOOKS.id)
    expect(booksGroup?.items).toEqual([])
  })

  it('soma todas as quantidades do catálogo', () => {
    expect(sumQuantities(items)).toBe(55)
  })

  it('soma as quantidades por categoria, só para as que têm item', () => {
    const byCategory = sumQuantitiesByCategory(items)

    expect(byCategory[CATEGORY_FOOD.id]).toBe(12)
    expect(byCategory[CATEGORY_CLOTHING.id]).toBe(3)
    expect(byCategory[CATEGORY_BOOKS.id]).toBeUndefined()
  })

  it('conta os itens por status calculado pelo backend', () => {
    expect(countByStatus(items, 'ok')).toBe(2)
    expect(countByStatus(items, 'low')).toBe(1)
    expect(countByStatus(items, 'out')).toBe(1)
  })

  it('lista os itens em falta ou com estoque baixo', () => {
    const needing = itemsNeedingRestock(items)

    expect(needing.map((i) => i.id)).toEqual(['clothing-1', 'footwear-1'])
  })

  it('calcula o progresso das metas: quantas foram atingidas entre as que têm meta', () => {
    expect(goalProgress(items)).toEqual({ reached: 0, total: 1 })

    const withReachedGoal = items.map((item) =>
      item.id === 'food-1' ? { ...item, quantity: 50 } : item,
    )
    expect(goalProgress(withReachedGoal)).toEqual({ reached: 1, total: 1 })
  })

  it('soma as cestas completas só das receitas ativas', () => {
    const templates = [
      { id: 't1', name: 'Cesta 1', items: [], isActive: true, feasibility: { completeBaskets: 3, missing: [] } },
      { id: 't2', name: 'Cesta 2', items: [], isActive: true, feasibility: { completeBaskets: 2, missing: [] } },
      { id: 't3', name: 'Cesta 3', items: [], isActive: false, feasibility: { completeBaskets: 10, missing: [] } },
    ] satisfies BasketTemplate[]

    expect(totalCompleteBaskets(templates)).toBe(5)
  })
})

describe('stockDraft — configurações', () => {
  it('monta o draft de configurações a partir do catálogo', () => {
    const draft = toSettingsDraft(items)

    expect(draft['food-1']).toEqual({ goal: 50, alertThreshold: DEFAULT_ALERT_THRESHOLD, isTracked: true })
    expect(draft['toys-1'].isTracked).toBe(false)
  })

  it('detecta só os itens com configuração alterada', () => {
    const saved = toSettingsDraft(items)
    const draft = { ...saved, 'food-1': { ...saved['food-1'], goal: 60 } }

    expect(changedSettingIds(saved, draft)).toEqual(['food-1'])
    expect(changedSettingIds(saved, saved)).toEqual([])
  })

  it('converte o draft de configurações para o payload da API', () => {
    const entries = toSettingEntries(toSettingsDraft(items))

    expect(entries).toContainEqual({
      stockItemId: 'food-1',
      goal: 50,
      alertThreshold: DEFAULT_ALERT_THRESHOLD,
      isTracked: true,
    })
  })

  it('valida a configuração: sem negativos e aviso não maior que a meta', () => {
    expect(settingError({ goal: 10, alertThreshold: 5, isTracked: true })).toBeNull()
    expect(settingError({ goal: 0, alertThreshold: 30, isTracked: true })).toBeNull()
    expect(settingError({ goal: -1, alertThreshold: 0, isTracked: true })).toMatch(/negativ/i)
    expect(settingError({ goal: 10, alertThreshold: 11, isTracked: true })).toMatch(/aviso/i)
  })
})

describe('stockDraft — regra de retirada', () => {
  it('lê a regra do ponto e usa anual / 3 quando o ponto não tem regra', () => {
    expect(toWithdrawalRule(pointWithRule)).toEqual({
      cycleType: 'Weekly',
      maxWithdrawalsPerCycle: 2,
    })
    expect(toWithdrawalRule({} as CollectionPoint)).toEqual({
      cycleType: 'Annual',
      maxWithdrawalsPerCycle: 3,
    })
  })

  it('detecta alteração no ciclo ou no máximo', () => {
    const saved = toWithdrawalRule(pointWithRule)

    expect(isRuleChanged(saved, saved)).toBe(false)
    expect(isRuleChanged(saved, { ...saved, cycleType: 'Monthly' })).toBe(true)
    expect(isRuleChanged(saved, { ...saved, maxWithdrawalsPerCycle: 3 })).toBe(true)
  })

  it('valida o máximo por ciclo entre 1 e 100', () => {
    const rule = toWithdrawalRule(pointWithRule)

    expect(ruleError(rule)).toBeNull()
    expect(ruleError({ ...rule, maxWithdrawalsPerCycle: 0 })).toMatch(/pelo menos 1/)
    expect(ruleError({ ...rule, maxWithdrawalsPerCycle: 101 })).toMatch(/100/)
  })
})
