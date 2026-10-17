import type { Role } from './auth'

/** UserResponse do backend. */
export interface UserAccount {
  id: string
  email: string
  fullName: string
  roles: Role[]
  collectionPointId: string | null
  isActive: boolean
  phoneNumber: string | null
}

export interface CreateUserInput {
  email: string
  password: string
  fullName: string
  phoneNumber?: string
  role: Role
  collectionPointId?: string | null
}

export interface ResetUserPasswordInput {
  newPassword: string
}

export interface UpdateUserInput {
  email: string
  fullName: string
  phoneNumber?: string
  role: Role
  collectionPointId?: string | null
}
