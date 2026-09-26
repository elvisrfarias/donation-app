import { History } from 'lucide-react'
import { useState } from 'react'
import type { Withdrawal } from '@/models/types'
import { Button } from '@/views/components/ui'
import { CategoryBadges } from './CategoryBadge'

const INITIAL_WITHDRAWALS_SHOWN = 5

const withdrawalDateFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

interface WithdrawalHistoryListProps {
  withdrawals: Withdrawal[]
  currentPointId: string
}

export function WithdrawalHistoryList({ withdrawals, currentPointId }: WithdrawalHistoryListProps) {
  const [showAll, setShowAll] = useState(false)
  const shown = showAll ? withdrawals : withdrawals.slice(0, INITIAL_WITHDRAWALS_SHOWN)
  const hiddenCount = withdrawals.length - shown.length

  return (
    <section aria-label="Histórico de retiradas" className="space-y-2">
      <h3 className="flex items-center gap-1.5 text-sm font-semibold text-stone-800">
        <History className="size-4 text-stone-400" />
        Histórico de retiradas
        <span className="text-xs font-medium text-stone-500">({withdrawals.length})</span>
      </h3>

      {withdrawals.length === 0 ? (
        <p className="rounded-xl bg-stone-50 px-3 py-2 text-sm text-stone-500">
          Este beneficiário ainda não fez nenhuma retirada.
        </p>
      ) : (
        <ul className="divide-y divide-stone-100 rounded-xl ring-1 ring-stone-200 ring-inset">
          {shown.map((withdrawal) => (
            <li
              key={withdrawal.id}
              className="flex flex-wrap items-center gap-x-3 gap-y-1 px-3 py-2"
            >
              <time
                dateTime={withdrawal.occurredAtUtc}
                className="text-xs font-medium text-stone-600 tabular-nums"
              >
                {withdrawalDateFormatter.format(new Date(withdrawal.occurredAtUtc))}
              </time>
              <CategoryBadges categories={withdrawal.categories} />
              <span className="text-xs text-stone-500">
                {withdrawal.pointId === currentPointId ? 'Neste ponto' : 'Em outro ponto'}
              </span>
              {withdrawal.notes && (
                <span
                  className="basis-full truncate text-xs text-stone-500"
                  title={withdrawal.notes}
                >
                  {withdrawal.notes}
                </span>
              )}
            </li>
          ))}
        </ul>
      )}

      {hiddenCount > 0 && (
        <Button variant="ghost" size="sm" onClick={() => setShowAll(true)}>
          Ver todas ({withdrawals.length})
        </Button>
      )}
    </section>
  )
}
