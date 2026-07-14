import { useState } from 'react'
import { Star } from 'lucide-react'
import { cn } from '@/lib/utils'

interface StarRatingProps {
  /** Nota atual (0 a 5). Aceita frações para exibição da média. */
  value: number
  /** Torna as estrelas clicáveis e reporta a nota escolhida. */
  onChange?: (value: number) => void
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

const sizeClasses = {
  sm: 'size-3.5',
  md: 'size-5',
  lg: 'size-7',
} as const

/**
 * Estrelas de avaliação. Sem `onChange` é apenas leitura (suporta média fracionada);
 * com `onChange` vira um seletor de 1 a 5 estrelas com destaque no hover.
 */
export function StarRating({ value, onChange, size = 'md', className }: StarRatingProps) {
  const [hover, setHover] = useState<number | null>(null)
  const interactive = !!onChange
  const shown = hover ?? value
  const star = sizeClasses[size]

  return (
    <span
      className={cn('inline-flex items-center gap-0.5', className)}
      role={interactive ? 'radiogroup' : 'img'}
      aria-label={interactive ? undefined : `Nota ${value.toFixed(1)} de 5`}
    >
      {[1, 2, 3, 4, 5].map((position) => {
        const filled = shown >= position
        const half = !filled && shown >= position - 0.5
        const icon = (
          <>
            <Star className={cn(star, 'text-stone-300')} />
            {(filled || half) && (
              <span
                className="absolute inset-0 overflow-hidden"
                style={half ? { width: '50%' } : undefined}
              >
                <Star className={cn(star, 'fill-amber-400 text-amber-400')} />
              </span>
            )}
          </>
        )

        if (!interactive) {
          return (
            <span key={position} className="relative inline-flex shrink-0">
              {icon}
            </span>
          )
        }

        return (
          <button
            key={position}
            type="button"
            aria-label={`${position} ${position === 1 ? 'estrela' : 'estrelas'}`}
            onClick={() => onChange(position)}
            onMouseEnter={() => setHover(position)}
            onMouseLeave={() => setHover(null)}
            className="relative shrink-0 cursor-pointer transition-transform hover:scale-110"
          >
            {icon}
          </button>
        )
      })}
    </span>
  )
}
