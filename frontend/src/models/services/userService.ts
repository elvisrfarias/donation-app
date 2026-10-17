import { http } from '@/models/api/httpClient'
import type {
  CreateUserInput,
  ResetUserPasswordInput,
  UpdateUserInput,
  UserAccount,
} from '@/models/types'

export const userService = {
  /** Lista todos os usuários gerenciais (requer role Admin). */
  async list(signal?: AbortSignal): Promise<UserAccount[]> {
    const { data } = await http.get<UserAccount[]>('/auth/users', { signal })
    return data
  },

  /** Cria um novo usuário gerencial (Admin ou PointManager) e devolve a conta criada. */
  async create(input: CreateUserInput): Promise<UserAccount> {
    const { data } = await http.post<UserAccount>('/auth/register', input)
    return data
  },

  /**
   * Atualiza e-mail, nome, telefone, papel e ponto de coleta de um usuário (Admin ou PointManager do ponto).
   * Trocar e-mail, papel ou ponto encerra as sessões dele.
   */
  async update(id: string, input: UpdateUserInput): Promise<UserAccount> {
    const { data } = await http.put<UserAccount>(`/auth/users/${id}`, input)
    return data
  },

  /** Desativa um usuário, bloqueando novos logins (requer role Admin). */
  async deactivate(id: string): Promise<void> {
    await http.post(`/auth/users/${id}/deactivate`)
  },

  /** Define uma nova senha para o usuário e encerra as sessões dele (Admin ou PointManager do ponto). */
  async resetPassword(id: string, input: ResetUserPasswordInput): Promise<void> {
    await http.post(`/auth/users/${id}/reset-password`, input)
  },
}
