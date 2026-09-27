import { cn } from '@/lib/utils'
import type { BeneficiaryWithdrawalHistory } from '@/models/types'
import { Badge } from '@/views/components/ui'

const MILLISECONDS_PER_DAY = 86_400_000
const LIMITED_LABEL = 'Cestas básicas'

const cycleDateFormatter = new Intl.DateTimeFormat('pt-BR', { timeZone: 'UTC' })

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

export function CycleUsage({ history }: { history: BeneficiaryWithdrawalHistory }) {
  const limitReached = history.remainingInCurrentCycle === 0
  const limitedLabel = LIMITED_LABEL

  return (
    <div className="space-y-3 rounded-2xl bg-stone-50 p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm font-semibold text-stone-800">
          {limitedLabel} no ciclo: {history.usedInCurrentCycle} de {history.maxPerCycle}
        </p>
        <Badge tone={limitReached ? 'red' : 'green'}>
          {limitReached
            ? 'Limite atingido'
            : `${history.remainingInCurrentCycle} ${history.remainingInCurrentCycle === 1 ? 'restante' : 'restantes'}`}
        </Badge>
      </div>
      <div
        role="progressbar"
        aria-label={`${limitedLabel} usadas no ciclo`}
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
      <p className="text-xs text-stone-500">
        {describeCycle(history)}. Só {limitedLabel.toLowerCase()} têm limite por ciclo; os demais
        itens não têm limite, mas ficam registrados no histórico.
      </p>
    </div>
  )
}
