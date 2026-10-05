import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

interface CardProps {
  className?: string
  children: ReactNode
}

export function Card({ className, children }: CardProps) {
  return (
    <div className={cn('shadow-card rounded-3xl border border-stone-200/60 bg-white', className)}>
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
          <span className="from-brand-500 to-brand-700 shadow-brand mt-0.5 grid size-11 place-items-center rounded-2xl bg-linear-to-br text-white">
            {icon}
          </span>
        )}
        <div>
          <h2 className="text-xl font-bold text-stone-900">{title}</h2>
          {description && <p className="mt-1 text-sm text-stone-500">{description}</p>}
        </div>
      </div>
      {action}
    </div>
  )
}
