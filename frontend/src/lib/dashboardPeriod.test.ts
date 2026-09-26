import { describe, expect, it } from 'vitest'
import { currentMonthPeriod, isSamePeriod, periodError } from './dashboardPeriod'

describe('dashboardPeriod', () => {
  it('o período padrão é o mês atual completo', () => {
    expect(currentMonthPeriod(new Date(2026, 5, 21))).toEqual({
      from: '2026-06-01',
      to: '2026-06-30',
    })
    expect(currentMonthPeriod(new Date(2026, 1, 3))).toEqual({
      from: '2026-02-01',
      to: '2026-02-28',
    })
    expect(currentMonthPeriod(new Date(2028, 1, 3))).toEqual({
      from: '2028-02-01',
      to: '2028-02-29',
    })
  })

  it('aceita um período em ordem e dentro de um ano', () => {
    expect(periodError({ from: '2026-06-10', to: '2026-06-10' })).toBeNull()
    expect(periodError({ from: '2026-01-01', to: '2026-12-31' })).toBeNull()
  })

  it('recusa datas ausentes, fora de ordem ou com mais de um ano', () => {
    expect(periodError({ from: '', to: '2026-06-10' })).toMatch(/informe/i)
    expect(periodError({ from: '2026-06-10', to: '2026-06-09' })).toMatch(/inicial/i)
    expect(periodError({ from: '2025-01-01', to: '2026-12-31' })).toMatch(/366/)
  })

  it('compara períodos', () => {
    expect(
      isSamePeriod(
        { from: '2026-06-01', to: '2026-06-30' },
        { from: '2026-06-01', to: '2026-06-30' },
      ),
    ).toBe(true)
    expect(
      isSamePeriod(
        { from: '2026-06-01', to: '2026-06-30' },
        { from: '2026-06-01', to: '2026-06-29' },
      ),
    ).toBe(false)
  })
})
