import type { BasketItemGap, BasketTemplate, StockItem } from '@/models/types'

export function activeBasketTemplates(templates: BasketTemplate[]): BasketTemplate[] {
  return templates.filter((template) => template.isActive)
}

export function inactiveBasketTemplates(templates: BasketTemplate[]): BasketTemplate[] {
  return templates.filter((template) => !template.isActive)
}

/** Quantas cestas o estoque atual de um item permite montar, isoladamente (o mesmo cálculo do backend). */
export function basketsPossibleForItem(gap: Pick<BasketItemGap, 'currentQuantity' | 'requiredPerBasket'>): number {
  if (gap.requiredPerBasket <= 0) return Number.POSITIVE_INFINITY
  return Math.floor(gap.currentQuantity / gap.requiredPerBasket)
}

/** Ids dos itens "gargalo": os que sozinhos limitam o total de cestas completas da receita. */
export function bottleneckItemIds(missing: BasketItemGap[], completeBaskets: number): Set<string> {
  return new Set(
    missing.filter((gap) => basketsPossibleForItem(gap) === completeBaskets).map((gap) => gap.itemId),
  )
}

/** Só os itens que ainda faltam (quantidade > 0) para montar mais uma cesta a partir da receita. */
export function missingForNextBasket(missing: BasketItemGap[]): BasketItemGap[] {
  return missing.filter((gap) => gap.neededForNextBasket > 0)
}

/** Item de estoque → unidade, para descrever quantidades faltantes com a unidade certa (ex.: "2 kg"). */
export type UnitByItemId = Record<string, string>

export function unitsByItemId(stockItems: StockItem[]): UnitByItemId {
  return Object.fromEntries(stockItems.map((item) => [item.id, item.unit]))
}

function formatAmount(amount: number, unit: string | undefined): string {
  if (!unit || unit.trim().toLowerCase() === 'un') return `${amount}`
  return `${amount} ${unit}`
}

/** Une uma lista de textos com vírgula e "e" antes do último item, como se espera em português. */
export function joinNaturally(parts: string[]): string {
  if (parts.length === 0) return ''
  if (parts.length === 1) return parts[0]
  if (parts.length === 2) return `${parts[0]} e ${parts[1]}`
  return `${parts.slice(0, -1).join(', ')} e ${parts[parts.length - 1]}`
}

/**
 * Frase pronta para o subtítulo do hero da aba Cestas: o que falta para montar mais uma cesta a
 * partir da receita selecionada. `null` quando não falta nada (estoque já abundante).
 */
export function describeNextBasketGap(missing: BasketItemGap[], units: UnitByItemId): string | null {
  const gaps = missingForNextBasket(missing)
  if (gaps.length === 0) return null

  const parts = gaps.map(
    (gap) => `${formatAmount(gap.neededForNextBasket, units[gap.itemId])} de ${gap.itemName}`,
  )
  return `Faltam ${joinNaturally(parts)} para montar mais 1 cesta.`
}

export interface InsufficientBasketItem {
  itemId: string
  itemName: string
  missingAmount: number
  unit?: string
}

/**
 * Itens que não têm estoque suficiente para montar `quantity` cestas desta receita agora — o
 * mesmo cálculo que o backend aplica ao registrar a retirada (`InsufficientStock`), mas calculado
 * no cliente para bloquear o envio antes de bater na API.
 */
export function insufficientItemsForQuantity(
  missing: BasketItemGap[],
  quantity: number,
  units: UnitByItemId = {},
): InsufficientBasketItem[] {
  if (quantity <= 0) return []
  return missing
    .map((gap) => ({
      itemId: gap.itemId,
      itemName: gap.itemName,
      unit: units[gap.itemId],
      missingAmount: Math.max(0, gap.requiredPerBasket * quantity - gap.currentQuantity),
    }))
    .filter((item) => item.missingAmount > 0)
}

export function isBasketFeasibleForQuantity(missing: BasketItemGap[], quantity: number): boolean {
  return quantity > 0 && insufficientItemsForQuantity(missing, quantity).length === 0
}

/** Um item em construção numa receita nova/editada — antes de existir como `BasketTemplateItem`. */
export interface RecipeDraftItem {
  stockItemId: string
  itemName: string
  unit: string
  quantity: number
}

/**
 * Preview ao vivo, no cliente, de quantas cestas completas a receita em construção permitiria
 * montar hoje — mesma fórmula do backend (`min` de `floor(estoque / quantidade)` por item), usada
 * enquanto o gestor monta a receita, antes de salvar.
 */
export function previewCompleteBaskets(
  items: RecipeDraftItem[],
  stockItemsById: Map<string, StockItem>,
): number {
  if (items.length === 0) return 0

  return items.reduce((min, item) => {
    if (item.quantity <= 0) return 0
    const stockItem = stockItemsById.get(item.stockItemId)
    if (!stockItem) return 0
    return Math.min(min, Math.floor(stockItem.quantity / item.quantity))
  }, Number.POSITIVE_INFINITY)
}
