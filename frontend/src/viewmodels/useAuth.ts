import { useAuthStore } from '@/stores/auth'

/** ViewModel de autenticação: estado do usuário + derivações de permissão para a View. */
export function useAuth() {
  const user = useAuthStore((s) => s.user)
  const status = useAuthStore((s) => s.status)
  const logout = useAuthStore((s) => s.logout)
  const hasRole = useAuthStore((s) => s.hasRole)

  return {
    user,
    isAuthenticated: status === 'authenticated',
    isAdmin: !!user?.roles.includes('Admin'),
    isManager: !!user && user.roles.some((r) => r === 'Admin' || r === 'PointManager'),
    hasRole,
    logout,
  }
}
