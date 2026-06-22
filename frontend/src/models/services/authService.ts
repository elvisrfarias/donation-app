import { http } from '@/models/api/httpClient'
import type { AuthResult, LoginInput } from '@/models/types/auth'

export const authService = {
  async login(input: LoginInput): Promise<AuthResult> {
    const { data } = await http.post<AuthResult>('/auth/login', input)
    return data
  },

  async refresh(refreshToken: string): Promise<AuthResult> {
    const { data } = await http.post<AuthResult>('/auth/refresh', { refreshToken })
    return data
  },

  async revoke(refreshToken: string): Promise<void> {
    await http.post('/auth/revoke', { refreshToken })
  },
}
