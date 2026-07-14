import { useState } from 'react'
import { PackageCheck } from 'lucide-react'
import {
  itemCategoryLabels,
  itemCategoryValues,
  type CollectionPoint,
  type ItemCategory,
} from '@/models/types'
import { useUpdateStock } from '@/viewmodels'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Button } from '@/views/components/ui'

interface Props {
  point: CollectionPoint | null
  onClose: () => void
}

export function StockEditorModal({ point, onClose }: Props) {
  return (
    <ModalComponent
      open={!!point}
      onOpenChange={(open) => !open && onClose()}
      title={point ? `Estoque · ${point.name}` : undefined}
      description="Informe a quantidade disponível de cada item. Aparece como “Disponível agora” no mapa público."
      size="md"
    >
      {point && <StockEditorBody key={point.id} point={point} onClose={onClose} />}
    </ModalComponent>
  )
}

function StockEditorBody({ point, onClose }: { point: CollectionPoint; onClose: () => void }) {
  const { updateStock, isPending, error } = useUpdateStock()

  const categories: ItemCategory[] = itemCategoryValues

  const initial = Object.fromEntries(
    categories.map((c) => [c, point.stock.find((s) => s.category === c)?.quantity ?? 0]),
  ) as Record<ItemCategory, number>

  const [quantities, setQuantities] = useState<Record<ItemCategory, number>>(initial)

  function setQuantity(category: ItemCategory, value: string) {
    const parsed = Math.max(0, Math.floor(Number(value) || 0))
    setQuantities((prev) => ({ ...prev, [category]: parsed }))
  }

  async function save() {
    const items = categories.map((category) => ({ category, quantity: quantities[category] ?? 0 }))
    await updateStock({ id: point.id, items })
    onClose()
  }

  return (
    <div className="space-y-4 py-1">
      {error && <Alert tone="error">{error.displayMessage}</Alert>}

      <ul className="space-y-2">
        {categories.map((category) => (
          <li key={category} className="flex items-center justify-between gap-3">
            <span className="text-sm font-medium text-stone-700">{itemCategoryLabels[category]}</span>
            <input
              type="number"
              min={0}
              inputMode="numeric"
              value={quantities[category] ?? 0}
              onChange={(e) => setQuantity(category, e.target.value)}
              aria-label={`Quantidade de ${itemCategoryLabels[category]}`}
              className="h-10 w-28 rounded-xl border border-stone-200 px-3 text-sm text-stone-900 transition-all focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10"
            />
          </li>
        ))}
      </ul>

      <Button
        variant="brand"
        leftIcon={<PackageCheck className="size-4" />}
        onClick={save}
        disabled={isPending}
        className="w-full"
      >
        {isPending ? 'Salvando…' : 'Salvar estoque'}
      </Button>
    </div>
  )
}
