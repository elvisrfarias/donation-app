import { describe, expect, it } from 'vitest'
import { isCompleteCpf, maskCpf, onlyCpfDigits } from './cpfInput'

describe('cpfInput', () => {
  it('aplica a máscara enquanto digita', () => {
    expect(maskCpf('1')).toBe('1')
    expect(maskCpf('1114')).toBe('111.4')
    expect(maskCpf('111444777')).toBe('111.444.777')
    expect(maskCpf('11144477735')).toBe('111.444.777-35')
  })

  it('ignora o que não é dígito e o excesso', () => {
    expect(maskCpf('111.444.777-35999')).toBe('111.444.777-35')
    expect(onlyCpfDigits('abc111.444')).toBe('111444')
  })

  it('reconhece o CPF completo', () => {
    expect(isCompleteCpf('111.444.777-35')).toBe(true)
    expect(isCompleteCpf('111.444.777')).toBe(false)
  })
})
