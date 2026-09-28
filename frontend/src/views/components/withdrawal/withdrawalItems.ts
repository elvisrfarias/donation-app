import type { CollectionPoint, StockItem } from '@/models/types'

/**
 * Só é possível retirar itens acompanhados (a mesma configuração da aba "Metas e avisos" do
 * estoque) e com quantidade em estoque agora — a mesma regra que o backend aplica ao registrar a
 * retirada, para nunca oferecer algo que já não existe.
 *
 * Estes itens livres (roupas, calçados…) nunca têm limite por ciclo, mas descontam do estoque do
 * ponto ao serem dados — quem está sujeita à regra de equidade é a cesta básica (`BasketTemplate`),
 * tratada à parte na seção "Cesta básica" do formulário de retirada.
 */
export function availableWithdrawalItems(point: CollectionPoint): StockItem[] {
  return point.stockItems.filter((item) => item.isTracked && item.quantity > 0)
}

/** Um item em construção na retirada livre — antes de existir como entrada do comando. */
export interface WithdrawalDraftItem {
  stockItemId: string
  itemName: string
  unit: string
  quantity: number
}

/**
 * Quanto de um item ainda pode ser dado nesta retirada agora: o estoque atual do ponto, somado de
 * volta com o que ESTA MESMA retirada já tinha reservado dele antes de editar — o backend devolve a
 * reserva antiga antes de validar a nova ao processar uma edição, e o cliente espelha esse
 * raciocínio para o feedback instantâneo. Na criação, `reservedByThisWithdrawal` é sempre 0.
 */
export function availableQuantityForItem(
  stockItem: Pick<StockItem, 'quantity'>,
  reservedByThisWithdrawal: number,
): number {
  return stockItem.quantity + reservedByThisWithdrawal
}

export interface InsufficientWithdrawalItem {
  stockItemId: string
  itemName: string
  unit?: string
  missingAmount: number
}

/**
 * Itens da retirada livre que, ao vivo, já não caberiam no estoque disponível — mesmo raciocínio de
 * `insufficientItemsForQuantity` da cesta, mas sem multiplicador de receita: aqui uma retirada só
 * pede a quantidade exata de cada item, então "insuficiente" é simplesmente quantidade > disponível
 * (considerando a reserva desta mesma retirada, na edição). Não deveria ser alcançável quando o
 * stepper de quantidade já limita ao disponível, mas fica como checagem defensiva antes do envio.
 */
export function insufficientWithdrawalItems(
  items: WithdrawalDraftItem[],
  stockItemsById: Map<string, StockItem>,
  reservedByStockItemId: Record<string, number> = {},
): InsufficientWithdrawalItem[] {
  return items
    .map((item) => {
      const stockItem = stockItemsById.get(item.stockItemId)
      const available = availableQuantityForItem(
        { quantity: stockItem?.quantity ?? 0 },
        reservedByStockItemId[item.stockItemId] ?? 0,
      )
      return {
        stockItemId: item.stockItemId,
        itemName: item.itemName,
        unit: stockItem?.unit ?? item.unit,
        missingAmount: Math.max(0, item.quantity - available),
      }
    })
    .filter((item) => item.missingAmount > 0)
}
