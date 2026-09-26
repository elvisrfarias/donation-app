import { Minus, Plus } from 'lucide-react'
import { itemCategoryLabels, type ItemCategory } from '@/models/types'
import { cn } from '@/lib/utils'
import { categoryIcons } from './categoryIcons'
import {
  MAX_QUANTITY,
  clampQuantity,
  parseQuantity,
  stockStatus,
  type ResolvedStockSetting,
  type StockStatus,
} from './stockDraft'

const statusStyles: Record<
  StockStatus,
  { label: string; pill: string; icon: string; bar: string }
> = {
  out: {
    label: 'Em falta',
    pill: 'bg-red-50 text-red-700 ring-red-200',
    icon: 'bg-red-50 text-red-600',
    bar: 'bg-red-500',
  },
  low: {
    label: 'Estoque baixo',
    pill: 'bg-amber-50 text-amber-700 ring-amber-200',
    icon: 'bg-amber-50 text-amber-600',
    bar: 'bg-amber-500',
  },
  ok: {
    label: 'Disponível',
    pill: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
    icon: 'bg-emerald-50 text-emerald-600',
    bar: 'bg-emerald-500',
  },
  goal: {
    label: 'Meta atingida',
    pill: 'bg-brand-50 text-brand-700 ring-brand-200',
    icon: 'bg-brand-50 text-brand-600',
    bar: 'bg-brand-600',
  },
}

interface StockItemCardProps {
  category: ItemCategory
  quantity: number
  savedQuantity: number
  setting: ResolvedStockSetting
  disabled: boolean
  onChange: (quantity: number) => void
}

const stepperButton =
  'grid size-10 shrink-0 place-items-center rounded-xl text-stone-600 ring-1 ring-stone-200 transition-colors ring-inset hover:bg-brand-50 hover:text-brand-700 hover:ring-brand-200 disabled:opacity-40 disabled:hover:bg-transparent disabled:hover:text-stone-600 disabled:hover:ring-stone-200'

function progressPercent(quantity: number, goal: number): number {
  return Math.min(100, Math.round((quantity / goal) * 100))
}

export function StockItemCard({
  category,
  quantity,
  savedQuantity,
  setting,
  disabled,
  onChange,
}: StockItemCardProps) {
  const label = itemCategoryLabels[category]
  const Icon = categoryIcons[category]
  const status = statusStyles[stockStatus(quantity, setting)]
  const changed = quantity !== savedQuantity
  const hasGoal = setting.goal > 0

  return (
    <li
      className={cn(
        'rounded-2xl border bg-white p-4 transition-colors',
        changed ? 'border-brand-300 ring-brand-100 ring-2' : 'border-stone-200',
      )}
    >
      <div className="flex items-center gap-3">
        <span className={cn('grid size-11 shrink-0 place-items-center rounded-xl', status.icon)}>
          <Icon className="size-5" />
        </span>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-semibold text-stone-900">{label}</p>
          <span
            className={cn(
              'mt-1 inline-flex rounded-full px-2 py-0.5 text-[11px] font-semibold ring-1 ring-inset',
              status.pill,
            )}
          >
            {status.label}
          </span>
        </div>
        {changed && (
          <span className="text-[11px] font-medium whitespace-nowrap text-stone-500">
            era {savedQuantity}
          </span>
        )}
      </div>

      {hasGoal && (
        <div className="mt-3">
          <div className="flex items-center justify-between text-[11px] font-medium text-stone-500">
            <span>Meta</span>
            <span className="tabular-nums">
              {quantity} / {setting.goal}
            </span>
          </div>
          <div
            role="progressbar"
            aria-label={`Progresso da meta de ${label}`}
            aria-valuemin={0}
            aria-valuemax={setting.goal}
            aria-valuenow={Math.min(quantity, setting.goal)}
            className="mt-1 h-1.5 overflow-hidden rounded-full bg-stone-100"
          >
            <div
              className={cn('h-full rounded-full transition-all', status.bar)}
              style={{ width: `${progressPercent(quantity, setting.goal)}%` }}
            />
          </div>
        </div>
      )}

      <div className="mt-3 flex items-center gap-2">
        <button
          type="button"
          aria-label={`Diminuir ${label}`}
          disabled={disabled || quantity <= 0}
          onClick={() => onChange(clampQuantity(quantity - 1))}
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
          aria-label={`Quantidade de ${label}`}
          className="focus:border-brand-500 focus:ring-brand-500/10 h-10 min-w-0 flex-1 [appearance:textfield] rounded-xl border border-stone-200 text-center text-base font-semibold text-stone-900 tabular-nums transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50 disabled:text-stone-400 [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
        />
        <button
          type="button"
          aria-label={`Aumentar ${label}`}
          disabled={disabled || quantity >= MAX_QUANTITY}
          onClick={() => onChange(clampQuantity(quantity + 1))}
          className={stepperButton}
        >
          <Plus className="size-4" />
        </button>
      </div>
    </li>
  )
}
