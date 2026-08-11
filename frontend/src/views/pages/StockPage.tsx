import { useMemo, useState } from 'react'
import { Boxes, PackageCheck } from 'lucide-react'
import {
  itemCategoryLabels,
  itemCategoryValues,
  type CollectionPoint,
  type ItemCategory,
} from '@/models/types'
import { useAuth, useCollectionPoints, useUpdateStock } from '@/viewmodels'
import { Alert, Button, Card, SectionTitle, Select } from '@/views/components/ui'

export function StockPage() {
  const { user, isManager } = useAuth()
  const { points, isLoading, isError, error } = useCollectionPoints()
  const [selectedId, setSelectedId] = useState('')

  const ownPointId = user?.collectionPointId ?? null
  const selectedPoint = useMemo(
    () => points.find((point) => point.id === (ownPointId ?? selectedId)) ?? null,
    [points, ownPointId, selectedId],
  )

  return (
    <div className="mx-auto w-[90%] max-w-3xl space-y-6">
      <SectionTitle
        icon={<Boxes className="size-5" />}
        title="Estoque"
        description="Informe o que cada ponto tem disponível agora. Aparece como “Disponível agora” no mapa público."
      />

      {isError && <Alert tone="error">{error?.displayMessage}</Alert>}

      {ownPointId ? (
        isLoading && <p className="text-sm text-stone-500">Carregando seu ponto…</p>
      ) : (
        <Card className="space-y-2 p-5">
          <label htmlFor="stock-point" className="text-sm font-medium text-stone-700">
            Ponto de coleta
          </label>
          <Select
            id="stock-point"
            value={selectedId}
            onChange={(event) => setSelectedId(event.target.value)}
            disabled={isLoading || points.length === 0}
          >
            <option value="">
              {isLoading ? 'Carregando pontos…' : 'Selecione um ponto…'}
            </option>
            {points.map((point) => (
              <option key={point.id} value={point.id}>
                {point.name} — {point.address.city}/{point.address.state}
              </option>
            ))}
          </Select>
        </Card>
      )}

      {selectedPoint && (
        <StockEditor key={selectedPoint.id} point={selectedPoint} canEdit={isManager} />
      )}
    </div>
  )
}

function StockEditor({ point, canEdit }: { point: CollectionPoint; canEdit: boolean }) {
  const { updateStock, isPending, error } = useUpdateStock()

  const initialQuantities = Object.fromEntries(
    itemCategoryValues.map((category) => [
      category,
      point.stock.find((entry) => entry.category === category)?.quantity ?? 0,
    ]),
  ) as Record<ItemCategory, number>

  const [quantities, setQuantities] = useState<Record<ItemCategory, number>>(initialQuantities)
  const [savedName, setSavedName] = useState<string | null>(null)

  function setQuantity(category: ItemCategory, value: string) {
    const parsed = Math.max(0, Math.floor(Number(value) || 0))
    setSavedName(null)
    setQuantities((previous) => ({ ...previous, [category]: parsed }))
  }

  async function save() {
    const items = itemCategoryValues.map((category) => ({
      category,
      quantity: quantities[category] ?? 0,
    }))
    await updateStock({ id: point.id, items })
    setSavedName(point.name)
  }

  return (
    <Card className="space-y-4 p-5">
      <div>
        <h3 className="text-base font-semibold text-stone-900">{point.name}</h3>
        <p className="text-sm text-stone-500">Quantidade disponível de cada item.</p>
      </div>

      {error && <Alert tone="error">{error.displayMessage}</Alert>}
      {savedName && <Alert tone="success">Estoque de {savedName} atualizado.</Alert>}

      <ul className="space-y-2">
        {itemCategoryValues.map((category) => (
          <li key={category} className="flex items-center justify-between gap-3">
            <span className="text-sm font-medium text-stone-700">
              {itemCategoryLabels[category]}
            </span>
            <input
              type="number"
              min={0}
              inputMode="numeric"
              value={quantities[category] ?? 0}
              disabled={!canEdit}
              onChange={(event) => setQuantity(category, event.target.value)}
              aria-label={`Quantidade de ${itemCategoryLabels[category]}`}
              className="h-10 w-28 rounded-xl border border-stone-200 px-3 text-sm text-stone-900 transition-all focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 disabled:cursor-not-allowed disabled:bg-stone-50 disabled:text-stone-400"
            />
          </li>
        ))}
      </ul>

      {canEdit ? (
        <Button
          variant="brand"
          leftIcon={<PackageCheck className="size-4" />}
          onClick={save}
          disabled={isPending}
          className="w-full"
        >
          {isPending ? 'Salvando…' : 'Salvar estoque'}
        </Button>
      ) : (
        <p className="text-sm text-stone-500">
          Apenas administradores e gestores de ponto podem editar o estoque.
        </p>
      )}
    </Card>
  )
}
