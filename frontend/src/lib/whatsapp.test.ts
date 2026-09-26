import { describe, expect, it } from 'vitest'
import { whatsappUrl } from './whatsapp'

describe('whatsappUrl', () => {
  it('monta o link do WhatsApp com o código do Brasil e só os dígitos', () => {
    expect(whatsappUrl('(11) 98765-4321')).toBe('https://wa.me/5511987654321')
    expect(whatsappUrl('11987654321')).toBe('https://wa.me/5511987654321')
  })

  it('funciona também com telefone fixo', () => {
    expect(whatsappUrl('(11) 3456-7890')).toBe('https://wa.me/551134567890')
  })
})
