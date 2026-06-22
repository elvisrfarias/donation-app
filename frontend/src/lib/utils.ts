import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

/** Une classes Tailwind resolvendo conflitos (ex.: `p-2` + `p-4` -> `p-4`). */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs))
}

const dateTimeFmt = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})
const dateFmt = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' })

export function formatDateTime(iso: string | Date): string {
  const d = typeof iso === 'string' ? new Date(iso) : iso
  return Number.isNaN(d.getTime()) ? '—' : dateTimeFmt.format(d)
}

export function formatDate(iso: string | Date | null | undefined): string {
  if (!iso) return '—'
  const d = typeof iso === 'string' ? new Date(iso) : iso
  return Number.isNaN(d.getTime()) ? '—' : dateFmt.format(d)
}

/** Iniciais para avatares (ex.: "Maria Silva" -> "MS"). */
export function initials(name: string): string {
  return name
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? '')
    .join('')
}

export const sleep = (ms: number): Promise<void> => new Promise((r) => setTimeout(r, ms))
