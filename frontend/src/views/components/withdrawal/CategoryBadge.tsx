import { itemCategoryLabels, type ItemCategory } from '@/models/types'
import { categoryIcons } from '@/views/components/stock/categoryIcons'

export function CategoryBadge({ category }: { category: ItemCategory }) {
  const Icon = categoryIcons[category]
  return (
    <span className="inline-flex items-center gap-1 rounded-full bg-stone-100 px-2 py-0.5 text-xs font-semibold text-stone-700 ring-1 ring-stone-200 ring-inset">
      <Icon className="size-3" aria-hidden />
      {itemCategoryLabels[category]}
    </span>
  )
}

export function CategoryBadges({ categories }: { categories: ItemCategory[] }) {
  return (
    <span className="flex flex-wrap items-center gap-1">
      {categories.map((category) => (
        <CategoryBadge key={category} category={category} />
      ))}
    </span>
  )
}
