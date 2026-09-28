import type { WithdrawalCategoryRef } from '@/models/types'
import { FALLBACK_ICON, ICON_BY_KEY } from '@/views/components/stock/categoryIcons'

export function CategoryBadge({ name, icon }: { name: string; icon: string }) {
  const Icon = ICON_BY_KEY[icon] ?? FALLBACK_ICON
  return (
    <span className="inline-flex items-center gap-1 rounded-full bg-stone-100 px-2 py-0.5 text-xs font-semibold text-stone-700 ring-1 ring-stone-200 ring-inset">
      <Icon className="size-3" aria-hidden />
      {name}
    </span>
  )
}

export function CategoryBadges({ categories }: { categories: WithdrawalCategoryRef[] }) {
  return (
    <span className="flex flex-wrap items-center gap-1">
      {categories.map((category) => (
        <CategoryBadge
          key={category.categoryId}
          name={category.categoryName}
          icon={category.categoryIcon}
        />
      ))}
    </span>
  )
}
