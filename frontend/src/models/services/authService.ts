import { http } from '@/models/api/httpClient'
import type {
  AuthResult,
  ChangePasswordInput,
  CompletePasswordResetInput,
  LoginInput,
  RequestPasswordResetInput,
} from '@/models/types/auth'

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

  /** Troca a senha do usuário logado. As outras sessões caem; a resposta traz os tokens da sessão atual. */
  async changePassword(input: ChangePasswordInput): Promise<AuthResult> {
    const { data } = await http.post<AuthResult>('/auth/change-password', input)
    return data
  },

  /** "Esqueci minha senha": a resposta é a mesma exista ou não conta com o e-mail. */
  async requestPasswordReset(input: RequestPasswordResetInput): Promise<void> {
    await http.post('/auth/forgot-password', input)
  },

  /** Define a nova senha com o token do link enviado por e-mail; todas as sessões do usuário caem. */
  async completePasswordReset(input: CompletePasswordResetInput): Promise<void> {
    await http.post('/auth/reset-password', input)
  },
}
