import type { CSSProperties, ReactNode } from 'react'
import { cn } from '@/lib/utils'
import { Card } from '@/views/components/ui'

/**
 * Acentos categóricos dos cards de indicador. `brand` segue a marca (white-label,
 * via tokens CSS); os demais são matizes fixos para diferenciar visualmente,
 * no estilo dos cards coloridos do mockup "Jobie".
 */
export type StatAccent = 'brand' | 'blue' | 'green' | 'amber'

const gradientStops: Record<StatAccent, [string, string]> = {
  brand: ['var(--color-brand-500)', 'var(--color-brand-700)'],
  blue: ['#3b82f6', '#2563eb'],
  green: ['#10b981', '#059669'],
  amber: ['#f59e0b', '#d97706'],
}

export function StatCard({
  label,
  value,
  icon,
  hint,
  accent = 'brand',
  gradient = false,
}: {
  label: string
  value: ReactNode
  icon?: ReactNode
  hint?: string
  accent?: StatAccent
  /** Card preenchido com gradiente colorido e texto branco (destaque). */
  gradient?: boolean
}) {
  if (gradient) {
    const [from, to] = gradientStops[accent]
    const style: CSSProperties = { backgroundImage: `linear-gradient(135deg, ${from}, ${to})` }
    return (
      <div
        className="shadow-lift relative overflow-hidden rounded-3xl p-5 text-white"
        style={style}
      >
        <span className="pointer-events-none absolute -top-8 -right-6 size-28 rounded-full bg-white/15 blur-xl" />
        <div className="relative flex items-center justify-between">
          <p className="text-sm font-medium text-white/85">{label}</p>
          {icon && (
            <span className="grid size-10 place-items-center rounded-2xl bg-white/20 text-white backdrop-blur-sm">
              {icon}
            </span>
          )}
        </div>
        <p className="relative mt-3 text-3xl font-bold tracking-tight tabular-nums">{value}</p>
        {hint && <p className="relative mt-1 text-xs text-white/75">{hint}</p>}
      </div>
    )
  }

  return (
    <Card className="group hover:shadow-lift p-5 transition-shadow">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-stone-500">{label}</p>
        {icon && (
          <span
            className={cn(
              'bg-brand-50 text-brand-600 grid size-10 place-items-center rounded-2xl transition-colors',
              'group-hover:bg-brand-600 group-hover:text-white',
            )}
          >
            {icon}
          </span>
        )}
      </div>
      <p className="mt-3 text-3xl font-bold tracking-tight text-stone-900 tabular-nums">{value}</p>
      {hint && <p className="mt-1 text-xs text-stone-400">{hint}</p>}
    </Card>
  )
}
