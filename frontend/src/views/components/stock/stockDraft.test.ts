import { describe, expect, it } from 'vitest'
import type { CollectionPoint } from '@/models/types'
import {
  DEFAULT_ALERT_THRESHOLD,
  MAX_QUANTITY,
  categoriesNeedingRestock,
  countChangedCategories,
  countChangedSettings,
  countByStatus,
  goalProgress,
  isRuleChanged,
  ruleError,
  toWithdrawalRule,
  parseQuantity,
  settingError,
  stockStatus,
  sumQuantities,
  toStockItems,
  toStockQuantities,
  toStockSettingItems,
  toStockSettings,
  trackedCategories,
  untrackedCategoriesWithStock,
} from './stockDraft'

const pointWithStock = {
  stock: [
    { category: 'Food', quantity: 12 },
    { category: 'Clothing', quantity: 3 },
  ],
  stockSettings: [
    { category: 'Food', goal: 50, alertThreshold: 10, isTracked: true },
    { category: 'Toys', goal: 0, alertThreshold: 5, isTracked: false },
  ],
  withdrawalRule: { type: 'Weekly', maxWithdrawalsPerCycle: 2 },
} as CollectionPoint

describe('stockDraft — quantidades', () => {
  it('monta as quantidades de todas as categorias, com zero para as sem estoque', () => {
    const quantities = toStockQuantities(pointWithStock)

    expect(quantities.Food).toBe(12)
    expect(quantities.Clothing).toBe(3)
    expect(quantities.Books).toBe(0)
  })

  it('interpreta o texto digitado como inteiro entre zero e o máximo', () => {
    expect(parseQuantity('')).toBe(0)
    expect(parseQuantity('abc')).toBe(0)
    expect(parseQuantity('-4')).toBe(0)
    expect(parseQuantity('7.9')).toBe(7)
    expect(parseQuantity(String(MAX_QUANTITY + 500))).toBe(MAX_QUANTITY)
  })

  it('conta apenas as categorias alteradas em relação ao estoque salvo', () => {
    const saved = toStockQuantities(pointWithStock)

    expect(countChangedCategories(saved, { ...saved, Food: 13, Toys: 2 })).toBe(2)
    expect(countChangedCategories(saved, saved)).toBe(0)
  })

  it('soma só os itens das categorias acompanhadas', () => {
    const quantities = { ...toStockQuantities(pointWithStock), Toys: 40 }

    expect(sumQuantities(quantities, toStockSettings(pointWithStock))).toBe(15)
  })

  it('converte as quantidades para o payload da API com todas as categorias', () => {
    const items = toStockItems(toStockQuantities(pointWithStock))

    expect(items).toHaveLength(11)
    expect(items).toContainEqual({ category: 'Food', quantity: 12 })
  })
})

describe('stockDraft — configurações', () => {
  it('acompanha por padrão só cestas básicas, roupas e calçados nas categorias sem configuração', () => {
    const settings = toStockSettings({ ...pointWithStock, stockSettings: [] })

    expect(settings.Clothing).toEqual({
      goal: 0,
      alertThreshold: DEFAULT_ALERT_THRESHOLD,
      isTracked: true,
    })
    expect(settings.Food.isTracked).toBe(true)
    expect(settings.Footwear.isTracked).toBe(true)
    expect(settings.Books).toEqual({
      goal: 0,
      alertThreshold: DEFAULT_ALERT_THRESHOLD,
      isTracked: false,
    })
    expect(settings.Hygiene.isTracked).toBe(false)
    expect(settings.Other.isTracked).toBe(false)
  })

  it('usa a configuração salva quando ela existe', () => {
    const settings = toStockSettings(pointWithStock)

    expect(settings.Food).toEqual({ goal: 50, alertThreshold: 10, isTracked: true })
    expect(settings.Toys.isTracked).toBe(false)
  })

  it('classifica a quantidade em falta, baixa, disponível ou meta atingida', () => {
    const setting = { goal: 50, alertThreshold: 10, isTracked: true }

    expect(stockStatus(0, setting)).toBe('out')
    expect(stockStatus(10, setting)).toBe('low')
    expect(stockStatus(11, setting)).toBe('ok')
    expect(stockStatus(50, setting)).toBe('goal')
  })

  it('sem meta, nunca marca a meta como atingida', () => {
    const noGoal = { goal: 0, alertThreshold: 5, isTracked: true }

    expect(stockStatus(1000, noGoal)).toBe('ok')
  })

  it('lista só as categorias acompanhadas', () => {
    const settings = toStockSettings(pointWithStock)

    expect(trackedCategories(settings)).toEqual(['Food', 'Clothing', 'Footwear'])
  })

  it('aponta as categorias não acompanhadas que ainda têm estoque', () => {
    const quantities = { ...toStockQuantities(pointWithStock), Toys: 4 }
    const settings = toStockSettings(pointWithStock)

    expect(untrackedCategoriesWithStock(quantities, settings)).toEqual(['Toys'])
    expect(untrackedCategoriesWithStock(toStockQuantities(pointWithStock), settings)).toEqual([])
  })

  it('conta as categorias visíveis por situação', () => {
    const quantities = toStockQuantities(pointWithStock)
    const settings = toStockSettings(pointWithStock)

    expect(countByStatus(quantities, settings, 'ok')).toBe(1)
    expect(countByStatus(quantities, settings, 'low')).toBe(1)
    expect(countByStatus(quantities, settings, 'out')).toBe(1)
  })

  it('lista as categorias que precisam de reposição (em falta ou baixas), só as acompanhadas', () => {
    const quantities = toStockQuantities(pointWithStock)
    const settings = toStockSettings(pointWithStock)

    const needing = categoriesNeedingRestock(quantities, settings)

    expect(needing).toContain('Clothing')
    expect(needing).toContain('Footwear')
    expect(needing).not.toContain('Books')
    expect(needing).not.toContain('Food')
    expect(needing).not.toContain('Toys')
  })

  it('calcula o progresso das metas: quantas foram atingidas entre as que têm meta', () => {
    const quantities = { ...toStockQuantities(pointWithStock), Food: 50 }
    const settings = toStockSettings(pointWithStock)

    expect(goalProgress(quantities, settings)).toEqual({ reached: 1, total: 1 })
    expect(goalProgress(toStockQuantities(pointWithStock), settings)).toEqual({
      reached: 0,
      total: 1,
    })
  })

  it('conta apenas as categorias com configuração alterada', () => {
    const saved = toStockSettings(pointWithStock)

    expect(countChangedSettings(saved, saved)).toBe(0)
    expect(
      countChangedSettings(saved, {
        ...saved,
        Food: { ...saved.Food, goal: 60 },
        Books: { ...saved.Books, isTracked: true },
      }),
    ).toBe(2)
  })

  it('valida a configuração: sem negativos e aviso não maior que a meta', () => {
    expect(settingError({ goal: 10, alertThreshold: 5, isTracked: true })).toBeNull()
    expect(settingError({ goal: 0, alertThreshold: 30, isTracked: true })).toBeNull()
    expect(settingError({ goal: -1, alertThreshold: 0, isTracked: true })).toMatch(/negativ/i)
    expect(settingError({ goal: 10, alertThreshold: 11, isTracked: true })).toMatch(/aviso/i)
  })

  it('converte as configurações para o payload da API com todas as categorias', () => {
    const items = toStockSettingItems(toStockSettings(pointWithStock))

    expect(items).toHaveLength(11)
    expect(items).toContainEqual({
      category: 'Food',
      goal: 50,
      alertThreshold: 10,
      isTracked: true,
    })
  })
})

describe('stockDraft — regra de retirada', () => {
  it('lê a regra do ponto e usa mensal / 1 quando o ponto não tem regra', () => {
    expect(toWithdrawalRule(pointWithStock)).toEqual({
      cycleType: 'Weekly',
      maxWithdrawalsPerCycle: 2,
    })
    expect(toWithdrawalRule({} as CollectionPoint)).toEqual({
      cycleType: 'Annual',
      maxWithdrawalsPerCycle: 3,
    })
  })

  it('detecta alteração no ciclo ou no máximo', () => {
    const saved = toWithdrawalRule(pointWithStock)

    expect(isRuleChanged(saved, saved)).toBe(false)
    expect(isRuleChanged(saved, { ...saved, cycleType: 'Monthly' })).toBe(true)
    expect(isRuleChanged(saved, { ...saved, maxWithdrawalsPerCycle: 3 })).toBe(true)
  })

  it('valida o máximo por ciclo entre 1 e 100', () => {
    const rule = toWithdrawalRule(pointWithStock)

    expect(ruleError(rule)).toBeNull()
    expect(ruleError({ ...rule, maxWithdrawalsPerCycle: 0 })).toMatch(/pelo menos 1/)
    expect(ruleError({ ...rule, maxWithdrawalsPerCycle: 101 })).toMatch(/100/)
  })
})
