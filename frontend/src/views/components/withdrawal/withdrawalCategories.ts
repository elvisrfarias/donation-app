import { itemCategoryValues, type CollectionPoint, type ItemCategory } from '@/models/types'
import { toStockQuantities, toStockSettings } from '@/views/components/stock/stockDraft'

export const LIMITED_CATEGORY: ItemCategory = 'Food'

export function isLimitedCategory(category: ItemCategory): boolean {
  return category === LIMITED_CATEGORY
}

/**
 * Só é possível retirar categorias acompanhadas pelo ponto (a mesma configuração da aba "Metas e
 * avisos" do estoque) e que tenham quantidade em estoque agora — a mesma regra que o backend
 * aplica ao registrar a retirada, para nunca oferecer algo que já não existe.
 */
export function availableWithdrawalCategories(point: CollectionPoint): ItemCategory[] {
  const quantities = toStockQuantities(point)
  const settings = toStockSettings(point)
  return itemCategoryValues.filter(
    (category) => settings[category].isTracked && quantities[category] > 0,
  )
}
