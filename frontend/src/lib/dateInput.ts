const MAX_DATE_DIGITS = 8
const ISO_DATE = /^(\d{4})-(\d{2})-(\d{2})$/
const BRAZILIAN_DATE = /^(\d{2})\/(\d{2})\/(\d{4})$/

export function maskBrazilianDate(text: string): string {
  const digits = text.replace(/\D/g, '').slice(0, MAX_DATE_DIGITS)
  const day = digits.slice(0, 2)
  const month = digits.slice(2, 4)
  const year = digits.slice(4)
  return [day, month, year].filter(Boolean).join('/')
}

function buildValidDate(year: number, month: number, day: number): Date | undefined {
  const date = new Date(year, month - 1, day)
  const isSameDate =
    date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
  return isSameDate ? date : undefined
}

function pad(value: number, length: number): string {
  return String(value).padStart(length, '0')
}

export function dateToIso(date: Date): string {
  return `${pad(date.getFullYear(), 4)}-${pad(date.getMonth() + 1, 2)}-${pad(date.getDate(), 2)}`
}

export function isoToDate(iso: string): Date | undefined {
  const match = ISO_DATE.exec(iso)
  if (!match) return undefined
  return buildValidDate(Number(match[1]), Number(match[2]), Number(match[3]))
}

export function isoToBrazilian(iso: string): string {
  const match = ISO_DATE.exec(iso)
  return match ? `${match[3]}/${match[2]}/${match[1]}` : ''
}

export function brazilianToIso(text: string): string | null {
  const match = BRAZILIAN_DATE.exec(text)
  if (!match) return null
  const date = buildValidDate(Number(match[3]), Number(match[2]), Number(match[1]))
  return date ? dateToIso(date) : null
}
