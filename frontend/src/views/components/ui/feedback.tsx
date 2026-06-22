import type { ReactNode } from 'react'
import { AlertTriangle, CheckCircle2, Info, Loader2, XCircle } from 'lucide-react'
import { cn } from '@/lib/utils'

export function Spinner({ className }: { className?: string }) {
  return <Loader2 className={cn('size-5 animate-spin text-brand-600', className)} aria-hidden />
}

export function CenteredSpinner({ label }: { label?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-12 text-slate-500">
      <Spinner className="size-7" />
      {label && <p className="text-sm">{label}</p>}
    </div>
  )
}

type AlertTone = 'info' | 'success' | 'error' | 'warning'

const alertStyles: Record<AlertTone, { wrap: string; icon: ReactNode }> = {
  info: { wrap: 'bg-brand-50 text-brand-800 ring-brand-200', icon: <Info className="size-5" /> },
  success: {
    wrap: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
    icon: <CheckCircle2 className="size-5" />,
  },
  error: { wrap: 'bg-red-50 text-red-800 ring-red-200', icon: <XCircle className="size-5" /> },
  warning: {
    wrap: 'bg-amber-50 text-amber-800 ring-amber-200',
    icon: <AlertTriangle className="size-5" />,
  },
}

export function Alert({
  tone = 'info',
  title,
  children,
  className,
}: {
  tone?: AlertTone
  title?: string
  children?: ReactNode
  className?: string
}) {
  const style = alertStyles[tone]
  return (
    <div className={cn('flex gap-3 rounded-lg p-3 text-sm ring-1 ring-inset', style.wrap, className)}>
      <span className="mt-0.5 shrink-0" aria-hidden>
        {style.icon}
      </span>
      <div className="space-y-0.5">
        {title && <p className="font-semibold">{title}</p>}
        {children && <div className="opacity-90">{children}</div>}
      </div>
    </div>
  )
}

type BadgeTone = 'brand' | 'green' | 'amber' | 'red' | 'slate'

const badgeStyles: Record<BadgeTone, string> = {
  brand: 'bg-brand-50 text-brand-700 ring-brand-200',
  green: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  amber: 'bg-amber-50 text-amber-700 ring-amber-200',
  red: 'bg-red-50 text-red-700 ring-red-200',
  slate: 'bg-slate-100 text-slate-600 ring-slate-200',
}

export function Badge({
  tone = 'slate',
  children,
  className,
}: {
  tone?: BadgeTone
  children: ReactNode
  className?: string
}) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset',
        badgeStyles[tone],
        className,
      )}
    >
      {children}
    </span>
  )
}

export function EmptyState({
  icon,
  title,
  description,
  action,
}: {
  icon?: ReactNode
  title: string
  description?: string
  action?: ReactNode
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 rounded-xl border border-dashed border-slate-300 bg-slate-50/60 px-6 py-12 text-center">
      {icon && <div className="grid size-12 place-items-center rounded-full bg-white text-slate-400 shadow-card">{icon}</div>}
      <div>
        <p className="font-semibold text-slate-700">{title}</p>
        {description && <p className="mt-1 text-sm text-slate-500">{description}</p>}
      </div>
      {action}
    </div>
  )
}
