import { describe, expect, it } from 'vitest'
import { changePasswordSchema, createUserSchema, resetUserPasswordSchema } from './schemas'

function issuePaths(result: { success: boolean; error?: { issues: { path: PropertyKey[] }[] } }) {
  return result.error?.issues.map((issue) => issue.path.join('.')) ?? []
}

describe('changePasswordSchema', () => {
  const valid = { currentPassword: 'SenhaAtual1', newPassword: 'SenhaNova2', confirmPassword: 'SenhaNova2' }

  it('aceita uma troca válida', () => {
    expect(changePasswordSchema.safeParse(valid).success).toBe(true)
  })

  it('recusa nova senha sem número', () => {
    const result = changePasswordSchema.safeParse({
      ...valid,
      newPassword: 'SenhaSemNumero',
      confirmPassword: 'SenhaSemNumero',
    })
    expect(issuePaths(result)).toContain('newPassword')
  })

  it('recusa confirmação diferente da nova senha', () => {
    const result = changePasswordSchema.safeParse({ ...valid, confirmPassword: 'Outra1234' })
    expect(issuePaths(result)).toContain('confirmPassword')
  })

  it('recusa nova senha igual à atual', () => {
    const result = changePasswordSchema.safeParse({
      currentPassword: 'SenhaAtual1',
      newPassword: 'SenhaAtual1',
      confirmPassword: 'SenhaAtual1',
    })
    expect(issuePaths(result)).toContain('newPassword')
  })
})

describe('resetUserPasswordSchema', () => {
  it('aceita senha dentro da política com confirmação igual', () => {
    const result = resetUserPasswordSchema.safeParse({
      newPassword: 'SenhaNova2',
      confirmPassword: 'SenhaNova2',
    })
    expect(result.success).toBe(true)
  })

  it('recusa senha curta', () => {
    const result = resetUserPasswordSchema.safeParse({ newPassword: 'abc1', confirmPassword: 'abc1' })
    expect(issuePaths(result)).toContain('newPassword')
  })
})

describe('createUserSchema — senha', () => {
  it('exige pelo menos um número, como a política do backend', () => {
    const result = createUserSchema.safeParse({
      email: 'novo@doacao.local',
      password: 'SenhaSemNumero',
      fullName: 'Novo',
      role: 'Admin',
    })
    expect(issuePaths(result)).toContain('password')
  })
})
