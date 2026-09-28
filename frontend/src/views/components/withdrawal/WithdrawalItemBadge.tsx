import type { WithdrawalItemRef } from '@/models/types'
import { FALLBACK_ICON, ICON_BY_KEY } from '@/views/components/stock/categoryIcons'

export function WithdrawalItemBadge({
  name,
  icon,
  quantity,
}: {
  name: string
  icon: string
  quantity: number
}) {
  const Icon = ICON_BY_KEY[icon] ?? FALLBACK_ICON
  return (
    <span className="inline-flex items-center gap-1 rounded-full bg-stone-100 px-2 py-0.5 text-xs font-semibold text-stone-700 ring-1 ring-stone-200 ring-inset">
      <Icon className="size-3" aria-hidden />
      {name} × {quantity}
    </span>
  )
}

export function WithdrawalItemBadges({ items }: { items: WithdrawalItemRef[] }) {
  return (
    <span className="flex flex-wrap items-center gap-1">
      {items.map((item) => (
        <WithdrawalItemBadge
          key={item.stockItemId}
          name={item.itemName}
          icon={item.categoryIcon}
          quantity={item.quantity}
        />
      ))}
    </span>
  )
}
