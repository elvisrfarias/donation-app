import {
  currentMonthPeriod,
  isSamePeriod,
  periodError,
  type DashboardPeriod,
} from '@/lib/dashboardPeriod'
import { Button, DatePicker } from '@/views/components/ui'

interface PeriodFilterProps {
  period: DashboardPeriod
  onChange: (period: DashboardPeriod) => void
}

export function PeriodFilter({ period, onChange }: PeriodFilterProps) {
  const error = periodError(period)
  const isCurrentMonth = isSamePeriod(period, currentMonthPeriod())

  return (
    <div className="space-y-1">
      <div role="group" aria-label="Período" className="flex flex-wrap items-center gap-2">
        <DatePicker
          id="dashboard-from"
          className="w-44"
          allowFuture
          invalid={error !== null}
          value={period.from}
          onValueChange={(from) => onChange({ ...period, from })}
        />
        <span className="text-sm text-stone-500">até</span>
        <DatePicker
          id="dashboard-to"
          className="w-44"
          allowFuture
          invalid={error !== null}
          value={period.to}
          onValueChange={(to) => onChange({ ...period, to })}
        />
        {!isCurrentMonth && (
          <Button variant="ghost" size="sm" onClick={() => onChange(currentMonthPeriod())}>
            Mês atual
          </Button>
        )}
      </div>
      {error && (
        <p role="alert" className="text-xs font-medium text-red-600">
          {error}
        </p>
      )}
    </div>
  )
}
