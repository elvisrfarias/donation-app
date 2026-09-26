import { describe, expect, it } from 'vitest'
import {
  brazilianToIso,
  dateToIso,
  isoToBrazilian,
  isoToDate,
  maskBrazilianDate,
} from './dateInput'

describe('dateInput', () => {
  it('aplica a máscara dd/mm/aaaa enquanto digita, ignorando o que não é número', () => {
    expect(maskBrazilianDate('1')).toBe('1')
    expect(maskBrazilianDate('123')).toBe('12/3')
    expect(maskBrazilianDate('1203')).toBe('12/03')
    expect(maskBrazilianDate('12032000')).toBe('12/03/2000')
    expect(maskBrazilianDate('12/03/2000999')).toBe('12/03/2000')
    expect(maskBrazilianDate('ab12')).toBe('12')
  })

  it('converte data brasileira válida para ISO', () => {
    expect(brazilianToIso('12/03/2000')).toBe('2000-03-12')
    expect(brazilianToIso('29/02/2024')).toBe('2024-02-29')
  })

  it('rejeita datas incompletas ou que não existem no calendário', () => {
    expect(brazilianToIso('')).toBeNull()
    expect(brazilianToIso('12/03')).toBeNull()
    expect(brazilianToIso('31/02/2024')).toBeNull()
    expect(brazilianToIso('29/02/2023')).toBeNull()
    expect(brazilianToIso('00/01/2000')).toBeNull()
    expect(brazilianToIso('10/13/2000')).toBeNull()
  })

  it('converte ISO para o formato brasileiro e vazio para vazio', () => {
    expect(isoToBrazilian('2000-03-12')).toBe('12/03/2000')
    expect(isoToBrazilian('')).toBe('')
    expect(isoToBrazilian('lixo')).toBe('')
  })

  it('cria a data no fuso local, sem deslocar o dia', () => {
    const date = isoToDate('2000-03-12')

    expect(date?.getFullYear()).toBe(2000)
    expect(date?.getMonth()).toBe(2)
    expect(date?.getDate()).toBe(12)
    expect(isoToDate('')).toBeUndefined()
  })

  it('formata a data local como ISO', () => {
    expect(dateToIso(new Date(2000, 2, 12, 23, 59))).toBe('2000-03-12')
    expect(dateToIso(new Date(1999, 11, 5))).toBe('1999-12-05')
  })
})
