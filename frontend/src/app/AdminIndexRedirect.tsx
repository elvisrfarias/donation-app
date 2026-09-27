import { Navigate } from 'react-router-dom'
import { useAuth } from '@/viewmodels'

/** Landing da seção /app/admin: Admin cai em Pontos de Coleta; PointManager, direto em Usuários. */
export function AdminIndexRedirect() {
  const { isAdmin } = useAuth()
  return <Navigate to={isAdmin ? 'collection-points' : 'users'} replace />
}
