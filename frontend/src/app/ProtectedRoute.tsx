import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import type { Role } from '@/models/types/auth'
import { useAuth } from '@/viewmodels/useAuth'

/** Guarda de rota: exige autenticação e, opcionalmente, uma das roles informadas. */
export function ProtectedRoute({ children, roles }: { children: ReactNode; roles?: Role[] }) {
  const { isAuthenticated, hasRole } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  if (roles && roles.length > 0 && !hasRole(...roles)) {
    return <Navigate to="/app" replace />
  }

  return children
}
