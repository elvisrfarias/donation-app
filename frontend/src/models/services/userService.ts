import { http } from '@/models/api/httpClient'
import type { CreateUserInput, UpdateUserInput, UserAccount } from '@/models/types'

export const userService = {
  /** Lista todos os usuários gerenciais (requer role Admin). */
  async list(signal?: AbortSignal): Promise<UserAccount[]> {
    const { data } = await http.get<UserAccount[]>('/auth/users', { signal })
    return data
  },

  /** Cria um novo usuário gerencial (requer role Admin). */
  async create(input: CreateUserInput): Promise<void> {
    await http.post('/auth/register', input)
  },

  /** Atualiza nome, papel e ponto de coleta de um usuário (requer role Admin). */
  async update(id: string, input: UpdateUserInput): Promise<UserAccount> {
    const { data } = await http.put<UserAccount>(`/auth/users/${id}`, input)
    return data
  },

  /** Desativa um usuário, bloqueando novos logins (requer role Admin). */
  async deactivate(id: string): Promise<void> {
    await http.post(`/auth/users/${id}/deactivate`)
  },
}
