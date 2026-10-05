import { HeartHandshake } from 'lucide-react'
import { cn } from '@/lib/utils'

export function AppLogo({
  className,
  compact = false,
  inverted = false,
}: {
  className?: string
  compact?: boolean
  inverted?: boolean
}) {
  return (
    <div className={cn('flex items-center gap-2.5', className)}>
      <span className="from-brand-400 to-brand-600 grid size-9 place-items-center rounded-xl bg-linear-to-br text-white shadow-sm ring-1 ring-white/20 ring-inset">
        <HeartHandshake className="size-5" />
      </span>
      {!compact && (
        <div className="leading-tight">
          <p
            className={cn(
              'text-[0.95rem] font-extrabold tracking-tight',
              inverted ? 'text-white' : 'text-stone-900',
            )}
          >
            Bon<span className="text-brand-500">dade</span>
          </p>
          <p
            className={cn(
              'text-[11px] font-medium',
              inverted ? 'text-stone-400' : 'text-stone-500',
            )}
          >
            Pontos de Coleta
          </p>
        </div>
      )}
    </div>
  )
}
