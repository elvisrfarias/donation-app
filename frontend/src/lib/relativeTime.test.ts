import { describe, expect, it } from 'vitest'
import { formatRelativeTime } from './relativeTime'

const now = new Date('2026-06-21T12:00:00Z')

describe('formatRelativeTime', () => {
  it('descreve o passado recente', () => {
    expect(formatRelativeTime('2026-06-21T11:59:40Z', now)).toBe('agora há pouco')
    expect(formatRelativeTime('2026-06-21T11:30:00Z', now)).toBe('há 30 minutos')
    expect(formatRelativeTime('2026-06-21T09:00:00Z', now)).toBe('há 3 horas')
  })

  it('usa "ontem" e dias até um mês', () => {
    expect(formatRelativeTime('2026-06-20T11:00:00Z', now)).toBe('ontem')
    expect(formatRelativeTime('2026-06-16T12:00:00Z', now)).toBe('há 5 dias')
  })

  it('volta à data completa depois de 30 dias', () => {
    expect(formatRelativeTime('2026-04-01T12:00:00Z', now)).toMatch(/2026/)
  })

  it('tolera data inválida', () => {
    expect(formatRelativeTime('não é data', now)).toBe('—')
  })
})
