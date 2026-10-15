import { describe, expect, it } from 'vitest'
import { appendSuggestion } from './donationText'

describe('appendSuggestion', () => {
  it('começa a descrição com a sugestão', () => {
    expect(appendSuggestion('', 'Arroz')).toBe('Arroz')
    expect(appendSuggestion('   ', 'Arroz')).toBe('Arroz')
  })

  it('acrescenta separando por vírgula', () => {
    expect(appendSuggestion('Arroz', 'Feijão')).toBe('Arroz, Feijão')
  })

  it('não repete o que já foi escrito, ignorando maiúsculas', () => {
    expect(appendSuggestion('2 pacotes de arroz', 'Arroz')).toBe('2 pacotes de arroz')
  })

  it('não duplica a vírgula final', () => {
    expect(appendSuggestion('Arroz, ', 'Feijão')).toBe('Arroz, Feijão')
  })
})
