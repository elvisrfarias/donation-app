import { describe, expect, it } from 'vitest'
import { isValidPhone, maskPhone } from './phoneInput'

describe('maskPhone', () => {
  it('aplica a máscara de celular enquanto digita', () => {
    expect(maskPhone('')).toBe('')
    expect(maskPhone('1')).toBe('(1')
    expect(maskPhone('11')).toBe('(11')
    expect(maskPhone('119')).toBe('(11) 9')
    expect(maskPhone('1198765')).toBe('(11) 9876-5')
    expect(maskPhone('11987654321')).toBe('(11) 98765-4321')
  })

  it('usa a máscara de fixo com 10 dígitos', () => {
    expect(maskPhone('1134567890')).toBe('(11) 3456-7890')
  })

  it('ignora o que não é dígito e o excesso', () => {
    expect(maskPhone('(11) 98765-4321999')).toBe('(11) 98765-4321')
    expect(maskPhone('abc')).toBe('')
  })
})

describe('isValidPhone', () => {
  it('aceita fixo (10 dígitos) e celular (11 dígitos), com ou sem máscara', () => {
    expect(isValidPhone('(11) 3456-7890')).toBe(true)
    expect(isValidPhone('(11) 98765-4321')).toBe(true)
    expect(isValidPhone('11987654321')).toBe(true)
  })

  it('recusa números incompletos ou vazios', () => {
    expect(isValidPhone('(11) 9876')).toBe(false)
    expect(isValidPhone('')).toBe(false)
  })
})
