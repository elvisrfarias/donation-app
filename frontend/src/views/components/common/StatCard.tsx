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
    <Card className="p-4">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-slate-500">{label}</p>
        {icon && <span className="text-brand-500">{icon}</span>}
      </div>
      <p className="mt-2 text-2xl font-bold text-slate-900">{value}</p>
      {hint && <p className="mt-1 text-xs text-slate-400">{hint}</p>}
    </Card>
  )
}
