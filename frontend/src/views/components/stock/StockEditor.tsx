import { useMemo, useState } from 'react'
import { Check, Loader2, MapPin, Plus, Search, ShoppingBasket } from 'lucide-react'
import {
  collectionPointTypeLabels,
  itemCategoryLabels,
  type CollectionPoint,
  type StockItem,
} from '@/models/types'
import { cn } from '@/lib/utils'
import { useRemoveStockItem } from '@/viewmodels'
import { Alert, Badge, Button, Card } from '@/views/components/ui'
import { categoryIcons } from './categoryIcons'
import { StockItemCard } from './StockItemCard'
import { StockItemFormModal } from './StockItemFormModal'
import {
  countByStatus,
  goalProgress,
  groupByCategory,
  itemsNeedingRestock,
  totalCompleteBaskets,
  type StockItemCategoryGroup,
} from './stockDraft'
import { useAutoSaveStockQuantities } from './useAutoSaveStockQuantities'

interface StockEditorProps {
  point: CollectionPoint
  canEdit: boolean
  onViewBaskets?: () => void
}

const DIACRITICS = /[̀-ͯ]/g

function normalize(text: string): string {
  return text.normalize('NFD').replace(DIACRITICS, '').toLowerCase()
}

function SummaryStat({
  label,
  value,
  tone,
  onClick,
}: {
  label: string
  value: string | number
  tone?: string
  onClick?: () => void
}) {
  const Tag = onClick ? 'button' : 'div'
  return (
    <Tag
      type={onClick ? 'button' : undefined}
      onClick={onClick}
      className={cn(
        'rounded-xl bg-stone-50 px-3 py-2 text-left',
        onClick && 'cursor-pointer transition-colors hover:bg-stone-100',
      )}
    >
      <p className={`text-xl font-bold tabular-nums ${tone ?? 'text-stone-900'}`}>{value}</p>
      <p className="text-[11px] font-medium text-stone-500">{label}</p>
    </Tag>
  )
}

function AutoSaveIndicator({
  status,
  errorMessage,
  onRetry,
}: {
  status: 'idle' | 'saving' | 'saved' | 'error'
  errorMessage: string | null
  onRetry: () => void
}) {
  if (status === 'saving') {
    return (
      <span className="flex items-center gap-1.5 text-xs font-medium text-stone-500">
        <Loader2 className="size-3.5 animate-spin" />
        Salvando…
      </span>
    )
  }
  if (status === 'saved') {
    return (
      <span className="text-brand-700 flex items-center gap-1.5 text-xs font-medium">
        <Check className="size-3.5" />
        Tudo salvo
      </span>
    )
  }
  if (status === 'error') {
    return (
      <span className="flex items-center gap-2 text-xs font-medium text-red-600">
        {errorMessage ?? 'Falha ao salvar'}
        <button type="button" onClick={onRetry} className="font-semibold underline hover:no-underline">
          Tentar novamente
        </button>
      </span>
    )
  }
  return null
}

function CategorySection({
  group,
  quantityFor,
  disabled,
  onQuantityChange,
  onCommit,
  onEdit,
  onRemove,
}: {
  group: StockItemCategoryGroup
  quantityFor: (item: StockItem) => number
  disabled: boolean
  onQuantityChange: (itemId: string, quantity: number) => void
  onCommit: () => void
  onEdit: (item: StockItem) => void
  onRemove: (item: StockItem) => void
}) {
  const Icon = categoryIcons[group.category]
  const attentionCount = itemsNeedingRestock(group.items).length

  return (
    <Card className="p-0">
      <div className="flex items-center gap-3 p-4">
        <span className="grid size-9 shrink-0 place-items-center rounded-xl bg-stone-100 text-stone-600">
          <Icon className="size-4.5" />
        </span>
        <span className="min-w-0 flex-1">
          <span className="block truncate text-sm font-bold text-stone-900">
            {itemCategoryLabels[group.category]}
          </span>
          <span className="block text-xs text-stone-500">
            {group.items.length} {group.items.length === 1 ? 'item' : 'itens'}
          </span>
        </span>
        {attentionCount > 0 && (
          <Badge tone="amber">
            {attentionCount} {attentionCount === 1 ? 'precisa' : 'precisam'} de atenção
          </Badge>
        )}
      </div>
      <ul className="border-t border-stone-100">
        {group.items.map((item) => (
          <StockItemCard
            key={item.id}
            item={item}
            quantity={quantityFor(item)}
            disabled={disabled}
            onChange={(quantity) => onQuantityChange(item.id, quantity)}
            onCommit={onCommit}
            onEdit={() => onEdit(item)}
            onRemove={() => onRemove(item)}
          />
        ))}
      </ul>
    </Card>
  )
}

export function StockEditor({ point, canEdit, onViewBaskets }: StockEditorProps) {
  const items = point.stockItems
  const { quantityFor, setQuantity, saveNow, status, errorMessage } = useAutoSaveStockQuantities(
    point.id,
    items,
  )
  const { removeStockItem, error: removeError, reset: resetRemove } = useRemoveStockItem()

  const [search, setSearch] = useState('')
  const [formItem, setFormItem] = useState<StockItem | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [removeTarget, setRemoveTarget] = useState<StockItem | null>(null)

  const trimmedSearch = search.trim()
  const isSearching = trimmedSearch.length > 0

  const allGroups = useMemo(() => groupByCategory(items), [items])
  const visibleGroups = useMemo(() => {
    if (!isSearching) return allGroups
    const term = normalize(trimmedSearch)
    return allGroups
      .map((group) => ({
        ...group,
        items: group.items.filter((item) => normalize(item.name).includes(term)),
      }))
      .filter((group) => group.items.length > 0)
  }, [allGroups, isSearching, trimmedSearch])

  const resultCount = visibleGroups.reduce((total, group) => total + group.items.length, 0)
  const goals = goalProgress(items)
  const completeBaskets = totalCompleteBaskets(point.basketTemplates)

  function openAddForm() {
    setFormItem(null)
    setFormOpen(true)
  }

  function openEditForm(item: StockItem) {
    setFormItem(item)
    setFormOpen(true)
  }

  async function confirmRemove(item: StockItem) {
    if (!window.confirm(`Remover "${item.name}" do catálogo de estoque?`)) return
    resetRemove()
    setRemoveTarget(item)
    try {
      await removeStockItem({ pointId: point.id, stockItemId: item.id })
      setRemoveTarget(null)
    } catch {
      setRemoveTarget(item)
    }
  }

  const removeConflictMessage =
    removeError?.isConflict && removeTarget
      ? `"${removeTarget.name}" está em uma receita de cesta — remova-o da receita antes de excluí-lo.`
      : (removeError?.displayMessage ?? null)

  return (
    <div className="space-y-4">
      <Card className="space-y-4 p-5">
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

        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
          <SummaryStat label="Itens no catálogo" value={items.length} />
          <SummaryStat label="Itens em falta" value={countByStatus(items, 'out')} tone="text-red-600" />
          <SummaryStat
            label="Itens com estoque baixo"
            value={countByStatus(items, 'low')}
            tone="text-amber-600"
          />
          <SummaryStat
            label="Cestas completas hoje"
            value={completeBaskets}
            tone="text-brand-700"
            onClick={onViewBaskets}
          />
        </div>

        {goals.total > 0 && (
          <p className="text-xs text-stone-500">
            {goals.reached} de {goals.total} itens com meta definida já atingiram a meta.
          </p>
        )}
      </Card>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative min-w-0 flex-1">
          <Search className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-stone-400" />
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Buscar item por nome…"
            aria-label="Buscar item de estoque por nome"
            className="focus:border-brand-500 focus:ring-brand-500/10 h-11 w-full rounded-xl border border-stone-200 pr-3.5 pl-10 text-sm text-stone-900 transition-all focus:ring-4 focus:outline-none"
          />
        </div>
        {canEdit && (
          <Button
            type="button"
            variant="secondary"
            leftIcon={<Plus className="size-4" />}
            onClick={openAddForm}
            className="shrink-0"
          >
            Adicionar item
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-center justify-between gap-2">
        {isSearching ? (
          <p className="text-xs font-medium text-stone-500">
            {resultCount} {resultCount === 1 ? 'resultado' : 'resultados'}
          </p>
        ) : (
          <span />
        )}
        {canEdit && (
          <AutoSaveIndicator status={status} errorMessage={errorMessage} onRetry={saveNow} />
        )}
      </div>

      {removeConflictMessage && <Alert tone="error">{removeConflictMessage}</Alert>}

      <div className="space-y-3">
        {visibleGroups.length === 0 && (
          <Alert tone="info" title={isSearching ? 'Nenhum item encontrado' : 'Catálogo vazio'}>
            {isSearching
              ? 'Tente buscar por outro nome.'
              : 'Adicione itens ao catálogo de estoque deste ponto.'}
          </Alert>
        )}

        {visibleGroups.map((group) => (
          <CategorySection
            key={group.category}
            group={group}
            quantityFor={quantityFor}
            disabled={!canEdit}
            onQuantityChange={setQuantity}
            onCommit={saveNow}
            onEdit={openEditForm}
            onRemove={confirmRemove}
          />
        ))}
      </div>

      {!canEdit && (
        <p className="text-sm text-stone-500">
          Apenas administradores e gestores de ponto podem editar o estoque.
        </p>
      )}

      {onViewBaskets && completeBaskets === 0 && point.basketTemplates.length > 0 && (
        <button
          type="button"
          onClick={onViewBaskets}
          className="text-brand-700 flex items-center gap-1.5 text-xs font-medium hover:underline"
        >
          <ShoppingBasket className="size-3.5" />
          Ver receitas de cesta na aba Cestas
        </button>
      )}

      <StockItemFormModal
        key={formItem?.id ?? 'new'}
        point={point}
        item={formItem}
        open={formOpen}
        onClose={() => setFormOpen(false)}
      />
    </div>
  )
}
