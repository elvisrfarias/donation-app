import type { Role } from './auth'

/** UserResponse do backend. */
export interface UserAccount {
  id: string
  email: string
  fullName: string
  roles: Role[]
  collectionPointId: string | null
  isActive: boolean
}

export interface CreateUserInput {
  email: string
  password: string
  fullName: string
  role: Role
  collectionPointId?: string | null
}

export interface UpdateUserInput {
  fullName: string
  role: Role
  collectionPointId?: string | null
}
