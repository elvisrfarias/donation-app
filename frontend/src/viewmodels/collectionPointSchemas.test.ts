import { describe, expect, it } from 'vitest'
import { createCollectionPointSchema, updateCollectionPointSchema } from './schemas'

const image = new File(['x'], 'ponto.png', { type: 'image/png' })

const validPoint = {
  name: 'Casa Solidária',
  type: 'Association' as const,
  street: 'Rua das Flores',
  number: '100',
  district: 'Centro',
  city: 'Concórdia',
  state: 'SC',
  zipCode: '89700-000',
  latitude: -27.2342,
  longitude: -52.0278,
  responsibleName: 'Maria',
  email: 'maria@doacao.org',
  phone: '(49) 98765-4321',
  image,
}

describe('createCollectionPointSchema — local exato', () => {
  it('aceita o ponto com o local marcado no mapa', () => {
    expect(createCollectionPointSchema.safeParse(validPoint).success).toBe(true)
  })

  it('recusa o ponto sem coordenadas, pedindo para marcar no mapa', () => {
    const result = createCollectionPointSchema.safeParse({
      ...validPoint,
      latitude: undefined,
      longitude: undefined,
    })

    expect(result.success).toBe(false)
    const messages = result.error?.issues.map((issue) => issue.message) ?? []
    expect(messages).toContain('Marque no mapa o local exato do ponto de coleta.')
  })

  it('recusa coordenadas fora dos limites', () => {
    expect(createCollectionPointSchema.safeParse({ ...validPoint, latitude: 91 }).success).toBe(
      false,
    )
    expect(createCollectionPointSchema.safeParse({ ...validPoint, longitude: -181 }).success).toBe(
      false,
    )
  })
})

describe('updateCollectionPointSchema — local exato', () => {
  it('também exige o local exato ao editar, mesmo sem trocar a imagem', () => {
    const withoutImage = { ...validPoint, image: undefined }

    expect(updateCollectionPointSchema.safeParse(withoutImage).success).toBe(true)
    expect(
      updateCollectionPointSchema.safeParse({ ...withoutImage, latitude: undefined }).success,
    ).toBe(false)
  })
})
