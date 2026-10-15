const MINUTE_IN_MS = 60_000
const HOUR_IN_MS = 60 * MINUTE_IN_MS
const DAY_IN_MS = 24 * HOUR_IN_MS
const MAX_RELATIVE_DAYS = 30

const formatter = new Intl.RelativeTimeFormat('pt-BR', { numeric: 'auto' })
const absoluteFormatter = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' })

/** Tempo decorrido em linguagem natural ("há 2 horas", "ontem"); passado de 30 dias, volta à data. */
export function formatRelativeTime(iso: string, now: Date = new Date()): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return '—'

  const elapsed = now.getTime() - date.getTime()
  if (elapsed < MINUTE_IN_MS) return 'agora há pouco'
  if (elapsed < HOUR_IN_MS) return formatter.format(-Math.floor(elapsed / MINUTE_IN_MS), 'minute')
  if (elapsed < DAY_IN_MS) return formatter.format(-Math.floor(elapsed / HOUR_IN_MS), 'hour')
  if (elapsed < MAX_RELATIVE_DAYS * DAY_IN_MS) {
    return formatter.format(-Math.floor(elapsed / DAY_IN_MS), 'day')
  }
  return absoluteFormatter.format(date)
}
