import { describe, expect, it } from 'vitest'
import type { Address } from '@/models/types'
import { addressSearchSteps } from './addressSearchSteps'

const address: Address = {
  street: 'Rua XV de Novembro',
  number: '1053',
  district: 'Industrial',
  city: 'Cascavel',
  state: 'PR',
  zipCode: '85801-462',
}

describe('addressSearchSteps', () => {
  it('vai do endereço completo até o centro da cidade', () => {
    expect(addressSearchSteps(address)).toEqual([
      {
        query: 'Rua XV de Novembro, 1053, Industrial, Cascavel - PR, 85801-462',
        approximate: false,
      },
      { query: 'Rua XV de Novembro, 1053, Cascavel - PR', approximate: false },
      { query: 'Cascavel - PR', approximate: true },
    ])
  })

  it('só a última etapa é aproximada', () => {
    const approximateSteps = addressSearchSteps(address).filter((step) => step.approximate)

    expect(approximateSteps).toHaveLength(1)
    expect(approximateSteps[0].query).toBe('Cascavel - PR')
  })

  it('não repete consultas iguais', () => {
    const minimal: Address = { ...address, district: '', zipCode: '' }

    const queries = addressSearchSteps(minimal).map((step) => step.query)

    expect(new Set(queries).size).toBe(queries.length)
  })
})
