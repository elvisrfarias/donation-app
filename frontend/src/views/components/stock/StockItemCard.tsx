import { Minus, Pencil, Plus, Trash2 } from 'lucide-react'
import type { StockItem, StockItemStatus } from '@/models/types'
import { cn } from '@/lib/utils'
import { MAX_QUANTITY, clampQuantity, parseQuantity } from './stockDraft'

const statusStyles: Record<StockItemStatus, { label: string; pill: string; bar: string }> = {
  out: {
    label: 'Em falta',
    pill: 'bg-red-50 text-red-700 ring-red-200',
    bar: 'bg-red-500',
  },
  low: {
    label: 'Estoque baixo',
    pill: 'bg-amber-50 text-amber-700 ring-amber-200',
    bar: 'bg-amber-500',
  },
  ok: {
    label: 'Disponível',
    pill: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
    bar: 'bg-emerald-500',
  },
  goal: {
    label: 'Meta atingida',
    pill: 'bg-brand-50 text-brand-700 ring-brand-200',
    bar: 'bg-brand-600',
  },
}

interface StockItemRowProps {
  item: StockItem
  quantity: number
  disabled: boolean
  onChange: (quantity: number) => void
  onCommit: () => void
  onEdit?: () => void
  onRemove?: () => void
}

const stepperButton =
  'grid size-9 shrink-0 place-items-center rounded-lg text-stone-600 ring-1 ring-stone-200 transition-colors ring-inset hover:bg-brand-50 hover:text-brand-700 hover:ring-brand-200 disabled:opacity-40 disabled:hover:bg-transparent disabled:hover:text-stone-600 disabled:hover:ring-stone-200'

const iconButton =
  'grid size-8 shrink-0 place-items-center rounded-lg text-stone-500 transition-colors hover:bg-stone-100 hover:text-stone-700'

function progressPercent(quantity: number, goal: number): number {
  return Math.min(100, Math.round((quantity / goal) * 100))
}

export function StockItemCard({
  item,
  quantity,
  disabled,
  onChange,
  onCommit,
  onEdit,
  onRemove,
}: StockItemRowProps) {
  const status = statusStyles[item.status]
  const hasGoal = item.goal > 0

  return (
    <li className="flex flex-wrap items-center gap-3 border-b border-stone-100 px-4 py-3 last:border-b-0">
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-1.5">
          <p className="truncate text-sm font-semibold text-stone-900">{item.name}</p>
          <span className="shrink-0 text-xs font-medium text-stone-500">{item.unit}</span>
          <span
            className={cn(
              'inline-flex shrink-0 rounded-full px-2 py-0.5 text-[11px] font-semibold ring-1 ring-inset',
              status.pill,
            )}
          >
            {status.label}
          </span>
        </div>
        {hasGoal && (
          <div className="mt-1.5 flex items-center gap-2">
            <div
              role="progressbar"
              aria-label={`Progresso da meta de ${item.name}`}
              aria-valuemin={0}
              aria-valuemax={item.goal}
              aria-valuenow={Math.min(quantity, item.goal)}
              className="h-1.5 w-24 overflow-hidden rounded-full bg-stone-100"
            >
              <div
                className={cn('h-full rounded-full transition-all', status.bar)}
                style={{ width: `${progressPercent(quantity, item.goal)}%` }}
              />
            </div>
            <span className="text-[11px] text-stone-500 tabular-nums">meta {item.goal}</span>
          </div>
        )}
      </div>

      <div className="flex shrink-0 items-center gap-1.5">
        <button
          type="button"
          aria-label={`Diminuir ${item.name}`}
          disabled={disabled || quantity <= 0}
          onClick={() => {
            onChange(clampQuantity(quantity - 1))
            onCommit()
          }}
          className={stepperButton}
        >
          <Minus className="size-4" />
        </button>
        <input
          type="number"
          min={0}
          max={MAX_QUANTITY}
          inputMode="numeric"
          value={quantity}
          disabled={disabled}
          onChange={(event) => onChange(parseQuantity(event.target.value))}
          onFocus={(event) => event.target.select()}
          onBlur={onCommit}
          aria-label={`Quantidade de ${item.name}`}
          className="focus:border-brand-500 focus:ring-brand-500/10 h-9 w-16 [appearance:textfield] rounded-lg border border-stone-200 text-center text-sm font-semibold text-stone-900 tabular-nums transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50 disabled:text-stone-500 [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
        />
        <button
          type="button"
          aria-label={`Aumentar ${item.name}`}
          disabled={disabled || quantity >= MAX_QUANTITY}
          onClick={() => {
            onChange(clampQuantity(quantity + 1))
            onCommit()
          }}
          className={stepperButton}
        >
          <Plus className="size-4" />
        </button>
      </div>

      {(onEdit || onRemove) && (
        <div className="flex shrink-0 items-center gap-1">
          {onEdit && (
            <button
              type="button"
              aria-label={`Editar ${item.name}`}
              onClick={onEdit}
              disabled={disabled}
              className={iconButton}
            >
              <Pencil className="size-4" />
            </button>
          )}
          {onRemove && (
            <button
              type="button"
              aria-label={`Remover ${item.name}`}
              onClick={onRemove}
              disabled={disabled}
              className={cn(iconButton, 'hover:bg-red-50 hover:text-red-600')}
            >
              <Trash2 className="size-4" />
            </button>
          )}
        </div>
      )}
    </li>
  )
}
