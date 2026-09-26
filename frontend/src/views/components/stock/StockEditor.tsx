import { useMemo, useState, type FormEvent } from 'react'
import { MapPin, PackageCheck, RotateCcw, SlidersHorizontal } from 'lucide-react'
import {
  collectionPointTypeLabels,
  itemCategoryLabels,
  type CollectionPoint,
  type ItemCategory,
} from '@/models/types'
import { useUpdateStock } from '@/viewmodels'
import { Alert, Badge, Button, Card } from '@/views/components/ui'
import { StockItemCard } from './StockItemCard'
import {
  categoriesNeedingRestock,
  countByStatus,
  countChangedCategories,
  goalProgress,
  sumQuantities,
  toStockItems,
  toStockQuantities,
  toStockSettings,
  trackedCategories,
  untrackedCategoriesWithStock,
  type StockQuantities,
} from './stockDraft'
import { useWarnBeforeLeaving } from './useWarnBeforeLeaving'

interface StockEditorProps {
  point: CollectionPoint
  canEdit: boolean
  onOpenSettings?: () => void
}

function SummaryStat({
  label,
  value,
  hint,
  tone,
}: {
  label: string
  value: string | number
  hint: string
  tone?: string
}) {
  return (
    <div className="rounded-2xl bg-stone-50 px-4 py-3">
      <p className={`text-2xl font-bold tabular-nums ${tone ?? 'text-stone-900'}`}>{value}</p>
      <p className="text-xs font-semibold text-stone-700">{label}</p>
      <p className="mt-0.5 text-[11px] leading-tight text-stone-500">{hint}</p>
    </div>
  )
}

function ofTotal(count: number, total: number): string {
  return `${count} de ${total}`
}

function formatCategoryList(categories: ItemCategory[]): string {
  return categories.map((category) => itemCategoryLabels[category]).join(', ')
}

export function StockEditor({ point, canEdit, onOpenSettings }: StockEditorProps) {
  const { updateStock, isPending, error, reset } = useUpdateStock()
  const savedQuantities = useMemo(() => toStockQuantities(point), [point])
  const settings = useMemo(() => toStockSettings(point), [point])
  const [draft, setDraft] = useState<StockQuantities>(savedQuantities)
  const [justSaved, setJustSaved] = useState(false)

  const changedCount = countChangedCategories(savedQuantities, draft)
  const isDirty = changedCount > 0
  useWarnBeforeLeaving(isDirty)

  const shownCategories = trackedCategories(settings)
  const hiddenWithStock = untrackedCategoriesWithStock(draft, settings)
  const trackedCount = shownCategories.length
  const needingRestock = categoriesNeedingRestock(savedQuantities, settings)
  const goals = goalProgress(savedQuantities, settings)

  function setQuantity(category: ItemCategory, quantity: number) {
    setJustSaved(false)
    reset()
    setDraft((previous) => ({ ...previous, [category]: quantity }))
  }

  function discardChanges() {
    setJustSaved(false)
    reset()
    setDraft(savedQuantities)
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!isDirty || isPending) return
    await updateStock({ id: point.id, items: toStockItems(draft) })
    setJustSaved(true)
  }

  return (
    <form onSubmit={save} className="space-y-4">
      <Card className="space-y-5 p-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <h3 className="truncate text-lg font-bold text-stone-900">{point.name}</h3>
            <p className="mt-1 flex items-center gap-1.5 text-sm text-stone-500">
              <MapPin className="size-4 shrink-0" />
              {point.address.city}/{point.address.state}
            </p>
          </div>
          <Badge tone="brand">{collectionPointTypeLabels[point.type]}</Badge>
        </div>

        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <SummaryStat
            label="Unidades em estoque"
            value={sumQuantities(savedQuantities, settings)}
            hint={`somando as ${trackedCount} categorias acompanhadas`}
          />
          <SummaryStat
            label="Categorias com meta atingida"
            value={goals.total > 0 ? ofTotal(goals.reached, goals.total) : '—'}
            hint={goals.total > 0 ? 'entre as que têm meta definida' : 'nenhuma meta definida'}
            tone="text-brand-700"
          />
          <SummaryStat
            label="Categorias com estoque baixo"
            value={ofTotal(countByStatus(savedQuantities, settings, 'low'), trackedCount)}
            hint="quantidade no aviso ou abaixo dele"
            tone="text-amber-600"
          />
          <SummaryStat
            label="Categorias em falta"
            value={ofTotal(countByStatus(savedQuantities, settings, 'out'), trackedCount)}
            hint="estoque zerado"
            tone="text-red-600"
          />
        </div>
      </Card>

      {needingRestock.length > 0 && (
        <Alert tone="warning" title="Reposição necessária">
          {formatCategoryList(needingRestock)}.
          {onOpenSettings && canEdit && (
            <button
              type="button"
              onClick={onOpenSettings}
              className="text-brand-700 ml-2 inline-flex items-center gap-1 font-semibold underline-offset-2 hover:underline"
            >
              <SlidersHorizontal className="size-3.5" />
              Ajustar avisos
            </button>
          )}
        </Alert>
      )}

      {error && <Alert tone="error">{error.displayMessage}</Alert>}
      {justSaved && !isDirty && (
        <Alert tone="success">Estoque atualizado. O mapa público já mostra os novos valores.</Alert>
      )}

      <ul className="grid gap-3 sm:grid-cols-2">
        {shownCategories.map((category) => (
          <StockItemCard
            key={category}
            category={category}
            quantity={draft[category]}
            savedQuantity={savedQuantities[category]}
            setting={settings[category]}
            disabled={!canEdit || isPending}
            onChange={(quantity) => setQuantity(category, quantity)}
          />
        ))}
      </ul>

      {trackedCount === 0 && (
        <Alert tone="info" title="Nenhuma categoria acompanhada">
          Ative as categorias que este ponto recebe na aba “Metas e avisos”.
        </Alert>
      )}

      {hiddenWithStock.length > 0 && (
        <p className="text-sm text-stone-500">
          Categorias não acompanhadas com estoque ({formatCategoryList(hiddenWithStock)}) ficam
          ocultas aqui e continuam no mapa público. Ative-as em “Metas e avisos”.
        </p>
      )}

      {canEdit ? (
        <div className="shadow-lift sticky bottom-20 z-10 flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-stone-200 bg-white/95 p-3 backdrop-blur-md lg:bottom-4">
          <p className="pl-2 text-sm font-medium text-stone-600">
            {isDirty
              ? `${changedCount} ${changedCount === 1 ? 'categoria alterada' : 'categorias alteradas'}`
              : 'Nenhuma alteração pendente'}
          </p>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              leftIcon={<RotateCcw className="size-4" />}
              onClick={discardChanges}
              disabled={!isDirty || isPending}
            >
              Descartar
            </Button>
            <Button
              type="submit"
              variant="brand"
              leftIcon={<PackageCheck className="size-4" />}
              loading={isPending}
              disabled={!isDirty}
            >
              {isPending ? 'Salvando…' : 'Salvar estoque'}
            </Button>
          </div>
        </div>
      ) : (
        <p className="text-sm text-stone-500">
          Apenas administradores e gestores de ponto podem editar o estoque.
        </p>
      )}
    </form>
  )
}
