import { describe, expect, it } from 'vitest'
import { registerBeneficiarySchema, updateBeneficiarySchema } from './schemas'

const valid = {
  name: 'Maria Souza',
  cpf: '111.444.777-35',
  email: 'maria@example.com',
  birthDate: '',
  phone: '(11) 98765-4321',
  secondaryPhone: '',
  registeredByPointId: 'point-1',
}

function errorMessages(result: { success: boolean; error?: { issues: { message: string }[] } }) {
  return result.error?.issues.map((issue) => issue.message) ?? []
}

describe('schemas de beneficiário', () => {
  it('aceita o cadastro só com o telefone principal', () => {
    expect(registerBeneficiarySchema.safeParse(valid).success).toBe(true)
  })

  it('exige o telefone de contato principal', () => {
    const result = registerBeneficiarySchema.safeParse({ ...valid, phone: '' })

    expect(errorMessages(result)).toContain('Informe o telefone de contato.')
  })

  it('exige o e-mail', () => {
    const result = registerBeneficiarySchema.safeParse({ ...valid, email: '' })

    expect(errorMessages(result)).toContain('E-mail é obrigatório.')
  })

  it('recusa e-mail inválido', () => {
    const result = registerBeneficiarySchema.safeParse({ ...valid, email: 'maria@' })

    expect(errorMessages(result)).toContain('E-mail inválido.')
  })

  it('aceita até dois telefones, celular ou fixo', () => {
    const result = registerBeneficiarySchema.safeParse({
      ...valid,
      secondaryPhone: '(11) 3456-7890',
    })

    expect(result.success).toBe(true)
  })

  it('recusa telefone incompleto', () => {
    const result = registerBeneficiarySchema.safeParse({ ...valid, phone: '(11) 9876' })

    expect(errorMessages(result)).toContain('Telefone inválido.')
  })

  it('recusa o mesmo telefone duas vezes', () => {
    const result = registerBeneficiarySchema.safeParse({
      ...valid,
      phone: '(11) 98765-4321',
      secondaryPhone: '11987654321',
    })

    expect(errorMessages(result)).toContain('Informe telefones diferentes.')
  })

  it('a edição também exige o e-mail e não pede o ponto', () => {
    const withoutPoint = { ...valid, registeredByPointId: undefined }

    expect(updateBeneficiarySchema.safeParse(withoutPoint).success).toBe(true)
    expect(updateBeneficiarySchema.safeParse({ ...withoutPoint, email: '' }).success).toBe(false)
  })
})
