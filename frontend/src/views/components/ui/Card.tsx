import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

interface CardProps {
  className?: string
  children: ReactNode
}

export function Card({ className, children }: CardProps) {
  return (
    <div className={cn('rounded-xl border border-slate-200 bg-white shadow-card', className)}>
      {children}
    </div>
  )
}

interface SectionTitleProps {
  title: string
  description?: string
  icon?: ReactNode
  action?: ReactNode
  className?: string
}

export function SectionTitle({ title, description, icon, action, className }: SectionTitleProps) {
  return (
    <div className={cn('flex items-start justify-between gap-4', className)}>
      <div className="flex items-start gap-3">
        {icon && (
          <span className="mt-0.5 grid size-9 place-items-center rounded-lg bg-brand-50 text-brand-600">
            {icon}
          </span>
        )}
        <div>
          <h2 className="text-lg font-semibold text-slate-900">{title}</h2>
          {description && <p className="mt-0.5 text-sm text-slate-500">{description}</p>}
        </div>
      </div>
      {action}
    </div>
  )
}
