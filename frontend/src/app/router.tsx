import { Suspense, type ReactNode } from 'react'
import { createBrowserRouter } from 'react-router-dom'
import { CenteredSpinner } from '@/views/components/ui'
import { lazyWithReload } from './lazyWithReload'
import { ProtectedRoute } from './ProtectedRoute'
import { RouteError } from './RouteError'

const MapPage = lazyWithReload(() =>
  import('@/views/pages/MapPage').then((m) => ({ default: m.MapPage })),
)
const LoginPage = lazyWithReload(() =>
  import('@/views/pages/LoginPage').then((m) => ({ default: m.LoginPage })),
)
const AppLayout = lazyWithReload(() =>
  import('@/views/layouts/AppLayout').then((m) => ({ default: m.AppLayout })),
)
const DashboardPage = lazyWithReload(() =>
  import('@/views/pages/DashboardPage').then((m) => ({ default: m.DashboardPage })),
)
const CollectionPointsPage = lazyWithReload(() =>
  import('@/views/pages/CollectionPointsPage').then((m) => ({ default: m.CollectionPointsPage })),
)
const StockPage = lazyWithReload(() =>
  import('@/views/pages/StockPage').then((m) => ({ default: m.StockPage })),
)
const FamiliesPage = lazyWithReload(() =>
  import('@/views/pages/FamiliesPage').then((m) => ({ default: m.FamiliesPage })),
)
const BeneficiariesPage = lazyWithReload(() =>
  import('@/views/pages/BeneficiariesPage').then((m) => ({ default: m.BeneficiariesPage })),
)
const RegisterWithdrawalPage = lazyWithReload(() =>
  import('@/views/pages/RegisterWithdrawalPage').then((m) => ({
    default: m.RegisterWithdrawalPage,
  })),
)
const NotFoundPage = lazyWithReload(() =>
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
  { path: '/', element: withSuspense(<MapPage />), errorElement: <RouteError /> },
  { path: '/login', element: withSuspense(<LoginPage />), errorElement: <RouteError /> },
  {
    path: '/app',
    element: (
      <ProtectedRoute>{withSuspense(<AppLayout />)}</ProtectedRoute>
    ),
    errorElement: <RouteError />,
    children: [
      { index: true, element: withSuspense(<DashboardPage />) },
      { path: 'collection-points', element: withSuspense(<CollectionPointsPage />) },
      { path: 'stock', element: withSuspense(<StockPage />) },
      { path: 'families', element: withSuspense(<FamiliesPage />) },
      { path: 'beneficiaries', element: withSuspense(<BeneficiariesPage />) },
      { path: 'withdrawals/new', element: withSuspense(<RegisterWithdrawalPage />) },
    ],
  },
  { path: '*', element: withSuspense(<NotFoundPage />), errorElement: <RouteError /> },
])
