import type { Category, CollectionPoint } from '@/models/types'

/**
 * Só é possível retirar categorias com algum item acompanhado (a mesma configuração da aba "Metas
 * e avisos" do estoque, agora por item) e com quantidade em estoque agora — a mesma regra que o
 * backend aplica ao registrar a retirada, para nunca oferecer algo que já não existe.
 *
 * Estas categorias livres (roupas, calçados…) nunca têm limite por ciclo nem dão baixa em
 * estoque — quem está sujeita à regra de equidade e consome estoque é a cesta básica
 * (`BasketTemplate`), tratada à parte na seção "Cesta básica" do formulário de retirada.
 */
export function availableWithdrawalCategories(point: CollectionPoint): Category[] {
  const availableCategoryIds = new Set(
    point.stockItems
      .filter((item) => item.isTracked && item.quantity > 0)
      .map((item) => item.categoryId),
  )
  return point.categories.filter((category) => availableCategoryIds.has(category.id))
}
