export type Role = 'Admin' | 'PointManager' | 'Viewer'

/** Resposta de autenticação do backend (AuthResult). */
export interface AuthResult {
  userId: string
  email: string
  fullName: string
  roles: Role[]
  collectionPointId: string | null
  accessToken: string
  refreshToken: string
  accessTokenExpiresAtUtc: string
  refreshTokenExpiresAtUtc: string
}

/** Dados do usuário mantidos no cliente (sem tokens). */
export interface AuthUser {
  userId: string
  email: string
  fullName: string
  roles: Role[]
  collectionPointId: string | null
}

export interface LoginInput {
  email: string
  password: string
}

export interface RequestPasswordResetInput {
  email: string
}

export interface CompletePasswordResetInput {
  token: string
  newPassword: string
}

export interface ChangePasswordInput {
  currentPassword: string
  newPassword: string
}

export const roleLabels: Record<Role, string> = {
  Admin: 'Administrador',
  PointManager: 'Gestor de Ponto',
  Viewer: 'Visualizador',
}
