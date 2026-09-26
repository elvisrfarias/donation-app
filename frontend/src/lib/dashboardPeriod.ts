import { dateToIso, isoToDate } from './dateInput'

export interface DashboardPeriod {
  from: string
  to: string
}

export const MAX_PERIOD_DAYS = 366
const MILLISECONDS_PER_DAY = 86_400_000

export function currentMonthPeriod(reference: Date = new Date()): DashboardPeriod {
  const firstDay = new Date(reference.getFullYear(), reference.getMonth(), 1)
  const lastDay = new Date(reference.getFullYear(), reference.getMonth() + 1, 0)
  return { from: dateToIso(firstDay), to: dateToIso(lastDay) }
}

export function isSamePeriod(a: DashboardPeriod, b: DashboardPeriod): boolean {
  return a.from === b.from && a.to === b.to
}

function daysBetween(from: Date, to: Date): number {
  return Math.round((to.getTime() - from.getTime()) / MILLISECONDS_PER_DAY)
}

export function periodError({ from, to }: DashboardPeriod): string | null {
  const start = isoToDate(from)
  const end = isoToDate(to)
  if (!start || !end) return 'Informe a data inicial e a data final.'
  if (start > end) return 'A data inicial não pode ser maior que a final.'
  if (daysBetween(start, end) >= MAX_PERIOD_DAYS) {
    return `O período não pode passar de ${MAX_PERIOD_DAYS} dias.`
  }
  return null
}
