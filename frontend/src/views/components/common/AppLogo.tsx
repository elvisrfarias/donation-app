import { HeartHandshake } from 'lucide-react'
import { cn } from '@/lib/utils'

export function AppLogo({ className, compact = false }: { className?: string; compact?: boolean }) {
  return (
    <div className={cn('flex items-center gap-2', className)}>
      <span className="grid size-9 place-items-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-white shadow-sm">
        <HeartHandshake className="size-5" />
      </span>
      {!compact && (
        <div className="leading-tight">
          <p className="text-sm font-extrabold tracking-tight text-slate-900">
            Doação<span className="text-brand-600">App</span>
          </p>
          <p className="text-[11px] text-slate-500">Pontos de Coleta</p>
        </div>
      )}
    </div>
  )
}
