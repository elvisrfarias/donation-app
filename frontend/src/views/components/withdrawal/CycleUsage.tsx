import { History } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { BeneficiaryWithdrawalHistory } from '@/models/types'
import { Badge } from '@/views/components/ui'

const RECENT_WITHDRAWALS_SHOWN = 3
const MILLISECONDS_PER_DAY = 86_400_000

const cycleDateFormatter = new Intl.DateTimeFormat('pt-BR', { timeZone: 'UTC' })
const withdrawalDateFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

function describeCycle(history: BeneficiaryWithdrawalHistory): string {
  const start = new Date(history.currentCycleStartUtc)
  const lastDay = new Date(new Date(history.currentCycleEndUtc).getTime() - MILLISECONDS_PER_DAY)
  return `Ciclo atual: ${cycleDateFormatter.format(start)} a ${cycleDateFormatter.format(lastDay)}`
}

function usageTone(
  history: BeneficiaryWithdrawalHistory,
): 'bg-emerald-500' | 'bg-amber-500' | 'bg-red-500' {
  if (history.remainingInCurrentCycle === 0) return 'bg-red-500'
  return history.remainingInCurrentCycle === 1 ? 'bg-amber-500' : 'bg-emerald-500'
}

function usagePercent(history: BeneficiaryWithdrawalHistory): number {
  return Math.min(100, (history.usedInCurrentCycle / history.maxPerCycle) * 100)
}

interface CycleUsageProps {
  history: BeneficiaryWithdrawalHistory
  currentPointId: string
}

export function CycleUsage({ history, currentPointId }: CycleUsageProps) {
  const limitReached = history.remainingInCurrentCycle === 0
  const recent = history.withdrawals.slice(0, RECENT_WITHDRAWALS_SHOWN)

  return (
    <div className="space-y-3 rounded-2xl bg-stone-50 p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm font-semibold text-stone-800">
          {history.usedInCurrentCycle} de {history.maxPerCycle}{' '}
          {history.maxPerCycle === 1 ? 'retirada usada' : 'retiradas usadas'}
        </p>
        <Badge tone={limitReached ? 'red' : 'green'}>
          {limitReached
            ? 'Limite atingido'
            : `${history.remainingInCurrentCycle} ${history.remainingInCurrentCycle === 1 ? 'restante' : 'restantes'}`}
        </Badge>
      </div>
      <div
        role="progressbar"
        aria-label="Retiradas usadas no ciclo"
        aria-valuemin={0}
        aria-valuemax={history.maxPerCycle}
        aria-valuenow={history.usedInCurrentCycle}
        className="h-2 overflow-hidden rounded-full bg-stone-200"
      >
        <div
          className={cn('h-full rounded-full transition-all', usageTone(history))}
          style={{ width: `${usagePercent(history)}%` }}
        />
      </div>
      <p className="text-xs text-stone-500">{describeCycle(history)}</p>

      {recent.length > 0 && (
        <div className="border-t border-stone-200 pt-3">
          <p className="flex items-center gap-1.5 text-xs font-semibold text-stone-600">
            <History className="size-3.5" />
            Últimas retiradas
          </p>
          <ul className="mt-1.5 space-y-1">
            {recent.map((withdrawal) => (
              <li key={withdrawal.id} className="flex justify-between gap-3 text-xs text-stone-600">
                <time dateTime={withdrawal.occurredAtUtc} className="tabular-nums">
                  {withdrawalDateFormatter.format(new Date(withdrawal.occurredAtUtc))}
                </time>
                <span className="text-stone-500">
                  {withdrawal.pointId === currentPointId ? 'Neste ponto' : 'Em outro ponto'}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
