import type { ReactNode } from 'react'
import { Card } from '@/views/components/ui'

export function StatCard({
  label,
  value,
  icon,
  hint,
}: {
  label: string
  value: ReactNode
  icon?: ReactNode
  hint?: string
}) {
  return (
    <Card className="group p-5 transition-shadow hover:shadow-lift">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-stone-500">{label}</p>
        {icon && (
          <span className="grid size-9 place-items-center rounded-xl bg-stone-100 text-stone-500 transition-colors group-hover:bg-brand-50 group-hover:text-brand-600">
            {icon}
          </span>
        )}
      </div>
      <p className="mt-3 text-3xl font-bold tracking-tight text-stone-900 tabular-nums">{value}</p>
      {hint && <p className="mt-1 text-xs text-stone-400">{hint}</p>}
    </Card>
  )
}
