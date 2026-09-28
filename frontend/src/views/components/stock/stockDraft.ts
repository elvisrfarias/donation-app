import type {
  BasketTemplate,
  Category,
  CollectionPoint,
  CycleType,
  StockItem,
  StockItemSettingEntry,
  StockItemStatus,
} from '@/models/types'

export const DEFAULT_ALERT_THRESHOLD = 5
export const MAX_WITHDRAWALS_PER_CYCLE = 100
export const DEFAULT_MAX_WITHDRAWALS_PER_CYCLE = 3
export const MAX_QUANTITY = 999_999

export function clampQuantity(value: number): number {
  if (!Number.isFinite(value)) return 0
  return Math.min(MAX_QUANTITY, Math.max(0, Math.floor(value)))
}

export function parseQuantity(text: string): number {
  return clampQuantity(Number(text))
}

export interface StockItemCategoryGroup {
  category: Category
  items: StockItem[]
}

/** Agrupa os itens do catálogo pelas categorias reais do ponto, ordenadas por nome — inclusive as
 *  que ainda não têm nenhum item, para que uma categoria recém-criada apareça vazia e o gestor
 *  possa adicionar o primeiro item nela. */
export function groupByCategory(items: StockItem[], categories: Category[]): StockItemCategoryGroup[] {
  return [...categories]
    .sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'))
    .map((category) => ({
      category,
      items: items.filter((item) => item.categoryId === category.id),
    }))
}

export function sumQuantities(items: StockItem[]): number {
  return items.reduce((total, item) => total + item.quantity, 0)
}

/** Soma as quantidades por categoria (chave = `categoryId`) — usado onde a UI ainda pensa em
 *  categorias (ex.: seleção de itens ao registrar uma retirada), não em itens individuais. Só
 *  inclui categorias com pelo menos um item no catálogo. */
export function sumQuantitiesByCategory(items: StockItem[]): Record<string, number> {
  const totals: Record<string, number> = {}
  for (const item of items) {
    totals[item.categoryId] = (totals[item.categoryId] ?? 0) + item.quantity
  }
  return totals
}

export function countByStatus(items: StockItem[], status: StockItemStatus): number {
  return items.filter((item) => item.status === status).length
}

/** Itens em falta ou com estoque baixo (o backend já classifica cada item — não recomputamos aqui). */
export function itemsNeedingRestock(items: StockItem[]): StockItem[] {
  return items.filter((item) => item.status === 'out' || item.status === 'low')
}

export function goalProgress(items: StockItem[]): { reached: number; total: number } {
  const withGoal = items.filter((item) => item.goal > 0)
  const reached = withGoal.filter((item) => item.quantity >= item.goal)
  return { reached: reached.length, total: withGoal.length }
}

/** Soma as cestas completas possíveis com o estoque atual, entre todas as receitas ativas do ponto. */
export function totalCompleteBaskets(templates: BasketTemplate[]): number {
  return templates
    .filter((template) => template.isActive)
    .reduce((total, template) => total + template.feasibility.completeBaskets, 0)
}

export interface ItemSetting {
  goal: number
  alertThreshold: number
  isTracked: boolean
}

/** Draft das metas/avisos/acompanhamento da aba "Metas e avisos": item → configuração. */
export type SettingsDraft = Record<string, ItemSetting>

export function toSettingsDraft(items: StockItem[]): SettingsDraft {
  return Object.fromEntries(
    items.map((item) => [
      item.id,
      { goal: item.goal, alertThreshold: item.alertThreshold, isTracked: item.isTracked },
    ]),
  )
}

/**
 * Reconcilia o draft de configurações com o catálogo atual: preserva as edições já feitas nos
 * itens que continuam existindo, inclui os itens novos (ex.: criados na aba Estoque, sem
 * remontar este componente) com a configuração salva, e descarta os que foram removidos.
 */
export function mergeSettingsDraft(previous: SettingsDraft, items: StockItem[]): SettingsDraft {
  return Object.fromEntries(
    items.map((item) => [
      item.id,
      item.id in previous
        ? previous[item.id]
        : { goal: item.goal, alertThreshold: item.alertThreshold, isTracked: item.isTracked },
    ]),
  )
}

export function changedSettingIds(saved: SettingsDraft, draft: SettingsDraft): string[] {
  return Object.keys(draft).filter((id) => {
    const before = saved[id]
    const after = draft[id]
    return (
      before.goal !== after.goal ||
      before.alertThreshold !== after.alertThreshold ||
      before.isTracked !== after.isTracked
    )
  })
}

export function toSettingEntries(draft: SettingsDraft): StockItemSettingEntry[] {
  return Object.entries(draft).map(([stockItemId, setting]) => ({ stockItemId, ...setting }))
}

export function settingError(setting: ItemSetting): string | null {
  if (setting.goal < 0 || setting.alertThreshold < 0) {
    return 'A meta e o aviso não podem ser negativos.'
  }
  if (setting.goal > 0 && setting.alertThreshold > setting.goal) {
    return 'O aviso não pode ser maior que a meta.'
  }
  return null
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
