import type { ReactNode } from 'react'

interface DashboardHeaderProps {
  title: string
  subtitle: string
  actions?: ReactNode
}

export function DashboardHeader({ title, subtitle, actions }: DashboardHeaderProps) {
  return (
    <header className="flex flex-wrap items-end justify-between gap-x-6 gap-y-3">
      <div className="min-w-0">
        <h1 className="text-2xl font-bold text-stone-900">{title}</h1>
        <p className="mt-0.5 truncate text-sm text-stone-500">{subtitle}</p>
      </div>
      {actions}
    </header>
  )
}
