import type { ReactNode } from 'react'
import { AlertTriangle, CheckCircle2, Info, Loader2, XCircle } from 'lucide-react'
import { cn } from '@/lib/utils'

export function Spinner({ className }: { className?: string }) {
  return <Loader2 className={cn('size-5 animate-spin text-brand-600', className)} aria-hidden />
}

export function CenteredSpinner({ label }: { label?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-16 text-stone-500">
      <Spinner className="size-7" />
      {label && <p className="text-sm">{label}</p>}
    </div>
  )
}

type AlertTone = 'info' | 'success' | 'error' | 'warning'

const alertStyles: Record<AlertTone, { wrap: string; icon: ReactNode }> = {
  info: { wrap: 'bg-brand-50 text-brand-900 ring-brand-100', icon: <Info className="size-5 text-brand-600" /> },
  success: {
    wrap: 'bg-emerald-50 text-emerald-900 ring-emerald-100',
    icon: <CheckCircle2 className="size-5 text-emerald-600" />,
  },
  error: { wrap: 'bg-red-50 text-red-900 ring-red-100', icon: <XCircle className="size-5 text-red-600" /> },
  warning: {
    wrap: 'bg-amber-50 text-amber-900 ring-amber-100',
    icon: <AlertTriangle className="size-5 text-amber-600" />,
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
    <div
      className={cn(
        'flex gap-3 rounded-xl p-3.5 text-sm ring-1 ring-inset',
        style.wrap,
        className,
      )}
    >
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
  brand: 'bg-brand-50 text-brand-700 ring-brand-100',
  green: 'bg-emerald-50 text-emerald-700 ring-emerald-100',
  amber: 'bg-amber-50 text-amber-700 ring-amber-100',
  red: 'bg-red-50 text-red-700 ring-red-100',
  slate: 'bg-stone-100 text-stone-600 ring-stone-200',
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
        'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-semibold ring-1 ring-inset',
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
    <div className="flex flex-col items-center justify-center gap-3 rounded-3xl border border-dashed border-stone-300 bg-white/60 px-6 py-14 text-center">
      {icon && (
        <div className="grid size-12 place-items-center rounded-2xl bg-white text-stone-400 shadow-card ring-1 ring-stone-100">
          {icon}
        </div>
      )}
      <div>
        <p className="font-semibold text-stone-800">{title}</p>
        {description && <p className="mt-1 text-sm text-stone-500">{description}</p>}
      </div>
      {action}
    </div>
  )
}
