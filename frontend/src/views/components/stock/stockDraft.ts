import {
  itemCategoryValues,
  type CollectionPoint,
  type CycleType,
  type ItemCategory,
  type StockItem,
  type StockSetting,
} from '@/models/types'

export type StockQuantities = Record<ItemCategory, number>
export type StockStatus = 'out' | 'low' | 'ok' | 'goal'

export interface ResolvedStockSetting {
  goal: number
  alertThreshold: number
  isTracked: boolean
}

export type StockSettings = Record<ItemCategory, ResolvedStockSetting>

export const DEFAULT_ALERT_THRESHOLD = 5
export const MAX_WITHDRAWALS_PER_CYCLE = 100
export const DEFAULT_MAX_WITHDRAWALS_PER_CYCLE = 3
export const MAX_QUANTITY = 999_999

const TRACKED_BY_DEFAULT: readonly ItemCategory[] = ['Food', 'Clothing', 'Footwear']

function defaultSettingFor(category: ItemCategory): ResolvedStockSetting {
  return {
    goal: 0,
    alertThreshold: DEFAULT_ALERT_THRESHOLD,
    isTracked: TRACKED_BY_DEFAULT.includes(category),
  }
}

export function toStockQuantities(point: CollectionPoint): StockQuantities {
  return Object.fromEntries(
    itemCategoryValues.map((category) => [
      category,
      point.stock.find((entry) => entry.category === category)?.quantity ?? 0,
    ]),
  ) as StockQuantities
}

export function toStockSettings(point: CollectionPoint): StockSettings {
  return Object.fromEntries(
    itemCategoryValues.map((category) => {
      const saved = point.stockSettings?.find((entry) => entry.category === category)
      return [
        category,
        saved
          ? { goal: saved.goal, alertThreshold: saved.alertThreshold, isTracked: saved.isTracked }
          : defaultSettingFor(category),
      ]
    }),
  ) as StockSettings
}

export function clampQuantity(value: number): number {
  if (!Number.isFinite(value)) return 0
  return Math.min(MAX_QUANTITY, Math.max(0, Math.floor(value)))
}

export function parseQuantity(text: string): number {
  return clampQuantity(Number(text))
}

export function stockStatus(quantity: number, setting: ResolvedStockSetting): StockStatus {
  if (quantity <= 0) return 'out'
  if (quantity <= setting.alertThreshold) return 'low'
  return setting.goal > 0 && quantity >= setting.goal ? 'goal' : 'ok'
}

export function trackedCategories(settings: StockSettings): ItemCategory[] {
  return itemCategoryValues.filter((category) => settings[category].isTracked)
}

export function untrackedCategoriesWithStock(
  quantities: StockQuantities,
  settings: StockSettings,
): ItemCategory[] {
  return itemCategoryValues.filter(
    (category) => !settings[category].isTracked && quantities[category] > 0,
  )
}

export function countChangedCategories(saved: StockQuantities, draft: StockQuantities): number {
  return itemCategoryValues.filter((category) => saved[category] !== draft[category]).length
}

export function countChangedSettings(saved: StockSettings, draft: StockSettings): number {
  return itemCategoryValues.filter((category) => {
    const before = saved[category]
    const after = draft[category]
    return (
      before.goal !== after.goal ||
      before.alertThreshold !== after.alertThreshold ||
      before.isTracked !== after.isTracked
    )
  }).length
}

export function sumQuantities(quantities: StockQuantities, settings: StockSettings): number {
  return trackedCategories(settings).reduce((total, category) => total + quantities[category], 0)
}

export function countByStatus(
  quantities: StockQuantities,
  settings: StockSettings,
  status: StockStatus,
): number {
  return trackedCategories(settings).filter(
    (category) => stockStatus(quantities[category], settings[category]) === status,
  ).length
}

export function categoriesNeedingRestock(
  quantities: StockQuantities,
  settings: StockSettings,
): ItemCategory[] {
  return itemCategoryValues.filter((category) => {
    if (!settings[category].isTracked) return false
    const status = stockStatus(quantities[category], settings[category])
    return status === 'out' || status === 'low'
  })
}

export function goalProgress(
  quantities: StockQuantities,
  settings: StockSettings,
): { reached: number; total: number } {
  const withGoal = itemCategoryValues.filter(
    (category) => settings[category].isTracked && settings[category].goal > 0,
  )
  const reached = withGoal.filter((category) => quantities[category] >= settings[category].goal)
  return { reached: reached.length, total: withGoal.length }
}

export function settingError(setting: ResolvedStockSetting): string | null {
  if (setting.goal < 0 || setting.alertThreshold < 0) {
    return 'A meta e o aviso não podem ser negativos.'
  }
  if (setting.goal > 0 && setting.alertThreshold > setting.goal) {
    return 'O aviso não pode ser maior que a meta.'
  }
  return null
}

export function toStockItems(quantities: StockQuantities): StockItem[] {
  return itemCategoryValues.map((category) => ({ category, quantity: quantities[category] }))
}

export function toStockSettingItems(settings: StockSettings): StockSetting[] {
  return itemCategoryValues.map((category) => ({ category, ...settings[category] }))
}

export interface WithdrawalRuleDraft {
  cycleType: CycleType
  maxWithdrawalsPerCycle: number
}

export function toWithdrawalRule(point: CollectionPoint): WithdrawalRuleDraft {
  return {
    cycleType: point.withdrawalRule?.type ?? 'Annual',
    maxWithdrawalsPerCycle:
      point.withdrawalRule?.maxWithdrawalsPerCycle ?? DEFAULT_MAX_WITHDRAWALS_PER_CYCLE,
  }
}

export function isRuleChanged(saved: WithdrawalRuleDraft, draft: WithdrawalRuleDraft): boolean {
  return (
    saved.cycleType !== draft.cycleType ||
    saved.maxWithdrawalsPerCycle !== draft.maxWithdrawalsPerCycle
  )
}

export function ruleError(rule: WithdrawalRuleDraft): string | null {
  if (rule.maxWithdrawalsPerCycle < 1) return 'O máximo por ciclo deve ser de pelo menos 1.'
  if (rule.maxWithdrawalsPerCycle > MAX_WITHDRAWALS_PER_CYCLE) {
    return `O máximo por ciclo não pode passar de ${MAX_WITHDRAWALS_PER_CYCLE}.`
  }
  return null
}
