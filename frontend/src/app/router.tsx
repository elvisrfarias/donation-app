/* eslint-disable react-refresh/only-export-components -- arquivo de rotas, não é alvo de fast-refresh */
import { lazy, Suspense, type ReactNode } from 'react'
import { createBrowserRouter } from 'react-router-dom'
import { CenteredSpinner } from '@/views/components/ui'
import { ProtectedRoute } from './ProtectedRoute'

// Code-splitting por rota: cada página vira um chunk próprio (o Leaflet, pesado,
// fica isolado no chunk do mapa e não pesa nas demais telas).
const MapPage = lazy(() => import('@/views/pages/MapPage').then((m) => ({ default: m.MapPage })))
const LoginPage = lazy(() =>
  import('@/views/pages/LoginPage').then((m) => ({ default: m.LoginPage })),
)
const AppLayout = lazy(() =>
  import('@/views/layouts/AppLayout').then((m) => ({ default: m.AppLayout })),
)
const DashboardPage = lazy(() =>
  import('@/views/pages/DashboardPage').then((m) => ({ default: m.DashboardPage })),
)
const CollectionPointsPage = lazy(() =>
  import('@/views/pages/CollectionPointsPage').then((m) => ({ default: m.CollectionPointsPage })),
)
const FamiliesPage = lazy(() =>
  import('@/views/pages/FamiliesPage').then((m) => ({ default: m.FamiliesPage })),
)
const RegisterWithdrawalPage = lazy(() =>
  import('@/views/pages/RegisterWithdrawalPage').then((m) => ({
    default: m.RegisterWithdrawalPage,
  })),
)
const NotFoundPage = lazy(() =>
  import('@/views/pages/NotFoundPage').then((m) => ({ default: m.NotFoundPage })),
)

function withSuspense(node: ReactNode): ReactNode {
  return (
    <Suspense
      fallback={
        <div className="grid min-h-dvh place-items-center">
          <CenteredSpinner />
        </div>
      }
    >
      {node}
    </Suspense>
  )
}

export const router = createBrowserRouter([
  { path: '/', element: withSuspense(<MapPage />) },
  { path: '/login', element: withSuspense(<LoginPage />) },
  {
    path: '/app',
    element: (
      <ProtectedRoute>{withSuspense(<AppLayout />)}</ProtectedRoute>
    ),
    children: [
      { index: true, element: withSuspense(<DashboardPage />) },
      { path: 'collection-points', element: withSuspense(<CollectionPointsPage />) },
      { path: 'families', element: withSuspense(<FamiliesPage />) },
      { path: 'withdrawals/new', element: withSuspense(<RegisterWithdrawalPage />) },
    ],
  },
  { path: '*', element: withSuspense(<NotFoundPage />) },
])
