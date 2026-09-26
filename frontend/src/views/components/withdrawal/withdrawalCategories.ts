import { itemCategoryValues, type CollectionPoint, type ItemCategory } from '@/models/types'
import { toStockQuantities } from '@/views/components/stock/stockDraft'

export const LIMITED_CATEGORY: ItemCategory = 'Food'

export function isLimitedCategory(category: ItemCategory): boolean {
  return category === LIMITED_CATEGORY
}

/** Só é possível retirar o que o ponto tem em estoque agora (quantidade maior que zero). */
export function availableWithdrawalCategories(point: CollectionPoint): ItemCategory[] {
  const quantities = toStockQuantities(point)
  return itemCategoryValues.filter((category) => quantities[category] > 0)
}
