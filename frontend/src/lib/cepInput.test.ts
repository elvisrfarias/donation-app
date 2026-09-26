import { describe, expect, it } from 'vitest'
import { isCompleteCep, maskCep } from './cepInput'

describe('cepInput', () => {
  it('aplica a máscara enquanto digita', () => {
    expect(maskCep('89')).toBe('89')
    expect(maskCep('89700')).toBe('89700')
    expect(maskCep('897000')).toBe('89700-0')
    expect(maskCep('89700000')).toBe('89700-000')
  })

  it('ignora o que não é dígito e o excesso', () => {
    expect(maskCep('89.700-000999')).toBe('89700-000')
    expect(maskCep('abc')).toBe('')
  })

  it('reconhece o CEP completo', () => {
    expect(isCompleteCep('89700-000')).toBe(true)
    expect(isCompleteCep('89700-00')).toBe(false)
  })
})
