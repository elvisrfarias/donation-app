import { describe, expect, it } from 'vitest'
import { registerBeneficiarySchema, updateBeneficiarySchema } from './schemas'

const valid = {
  name: 'Maria Souza',
  cpf: '111.444.777-35',
  email: 'maria@example.com',
  birthDate: '1990-05-20',
  phone: '(11) 98765-4321',
  secondaryPhone: '',
  zipCode: '',
  street: '',
  number: '',
  complement: '',
  district: '',
  city: '',
  state: '',
  registeredByPointId: 'point-1',
}

function errorMessages(result: { success: boolean; error?: { issues: { message: string }[] } }) {
  return result.error?.issues.map((issue) => issue.message) ?? []
}

describe('schemas de beneficiário', () => {
  it('aceita o cadastro só com o telefone principal', () => {
    expect(registerBeneficiarySchema.safeParse(valid).success).toBe(true)
  })

  it('exige a data de nascimento', () => {
    const result = registerBeneficiarySchema.safeParse({ ...valid, birthDate: '' })

    expect(errorMessages(result)).toContain('Data de nascimento é obrigatória.')
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

  describe('endereço', () => {
    const address = {
      zipCode: '89700-000',
      street: 'Rua das Flores',
      number: '100',
      complement: '',
      district: 'Centro',
      city: 'Concórdia',
      state: 'SC',
    }

    it('é opcional: o cadastro sem nenhum campo do endereço é válido', () => {
      expect(registerBeneficiarySchema.safeParse(valid).success).toBe(true)
    })

    it('aceita o endereço completo, com complemento opcional', () => {
      expect(registerBeneficiarySchema.safeParse({ ...valid, ...address }).success).toBe(true)
    })

    it('quando começa a preencher, exige os campos principais', () => {
      const result = registerBeneficiarySchema.safeParse({ ...valid, street: 'Rua das Flores' })
      const paths = result.error?.issues.map((issue) => issue.path.join('.')) ?? []

      expect(paths).toEqual(
        expect.arrayContaining(['zipCode', 'number', 'district', 'city', 'state']),
      )
      expect(paths).not.toContain('street')
      expect(paths).not.toContain('complement')
    })

    it('recusa CEP incompleto e UF com tamanho errado', () => {
      const result = registerBeneficiarySchema.safeParse({
        ...valid,
        ...address,
        zipCode: '89700-0',
        state: 'SCC',
      })
      const paths = result.error?.issues.map((issue) => issue.path.join('.')) ?? []

      expect(paths).toEqual(expect.arrayContaining(['zipCode', 'state']))
    })

    it('a edição também valida o endereço', () => {
      const update = { ...valid, registeredByPointId: undefined, city: 'Concórdia' }

      expect(updateBeneficiarySchema.safeParse(update).success).toBe(false)
    })
  })
})
