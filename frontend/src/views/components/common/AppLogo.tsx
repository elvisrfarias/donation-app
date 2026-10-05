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
      <span
        className={cn(
          'grid size-9 place-items-center rounded-xl shadow-sm ring-1 ring-inset',
          inverted
            ? 'text-brand-700 bg-white ring-white/40'
            : 'from-brand-400 to-brand-600 bg-linear-to-br text-white ring-white/20',
        )}
      >
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
            Bon<span className={inverted ? 'text-brand-200' : 'text-brand-500'}>dade</span>
          </p>
          <p
            className={cn('text-[11px] font-medium', inverted ? 'text-white/70' : 'text-stone-500')}
          >
            Pontos de Coleta
          </p>
        </div>
      )}
    </div>
  )
}
