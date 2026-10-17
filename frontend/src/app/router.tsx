import { Suspense, type ReactNode } from 'react'
import { createBrowserRouter, Navigate, Outlet } from 'react-router-dom'
import { CenteredSpinner } from '@/views/components/ui'
import { AdminIndexRedirect } from './AdminIndexRedirect'
import { lazyWithReload } from './lazyWithReload'
import { ProtectedRoute } from './ProtectedRoute'
import { RouteError } from './RouteError'

const MapPage = lazyWithReload(() =>
  import('@/views/pages/MapPage').then((m) => ({ default: m.MapPage })),
)
const LoginPage = lazyWithReload(() =>
  import('@/views/pages/LoginPage').then((m) => ({ default: m.LoginPage })),
)
const ForgotPasswordPage = lazyWithReload(() =>
  import('@/views/pages/ForgotPasswordPage').then((m) => ({ default: m.ForgotPasswordPage })),
)
const ResetPasswordPage = lazyWithReload(() =>
  import('@/views/pages/ResetPasswordPage').then((m) => ({ default: m.ResetPasswordPage })),
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
const DonationsPage = lazyWithReload(() =>
  import('@/views/pages/DonationsPage').then((m) => ({ default: m.DonationsPage })),
)
const NoticesPage = lazyWithReload(() =>
  import('@/views/pages/NoticesPage').then((m) => ({ default: m.NoticesPage })),
)
const BeneficiariesPage = lazyWithReload(() =>
  import('@/views/pages/BeneficiariesPage').then((m) => ({ default: m.BeneficiariesPage })),
)
const RegisterWithdrawalPage = lazyWithReload(() =>
  import('@/views/pages/RegisterWithdrawalPage').then((m) => ({
    default: m.RegisterWithdrawalPage,
  })),
)
const UsersPage = lazyWithReload(() =>
  import('@/views/pages/UsersPage').then((m) => ({ default: m.UsersPage })),
)
const SponsorsPage = lazyWithReload(() =>
  import('@/views/pages/SponsorsPage').then((m) => ({ default: m.SponsorsPage })),
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
    path: '/forgot-password',
    element: withSuspense(<ForgotPasswordPage />),
    errorElement: <RouteError />,
  },
  {
    path: '/reset-password',
    element: withSuspense(<ResetPasswordPage />),
    errorElement: <RouteError />,
  },
  {
    path: '/app',
    element: <ProtectedRoute>{withSuspense(<AppLayout />)}</ProtectedRoute>,
    errorElement: <RouteError />,
    children: [
      { index: true, element: withSuspense(<DashboardPage />) },
      {
        path: 'admin',
        element: (
          <ProtectedRoute roles={['Admin', 'PointManager']}>
            <Outlet />
          </ProtectedRoute>
        ),
        children: [
          { index: true, element: <AdminIndexRedirect /> },
          {
            path: 'collection-points',
            element: withSuspense(<CollectionPointsPage />),
          },
          { path: 'users', element: withSuspense(<UsersPage />) },
          {
            path: 'sponsors',
            element: (
              <ProtectedRoute roles={['Admin']}>{withSuspense(<SponsorsPage />)}</ProtectedRoute>
            ),
          },
        ],
      },
      { path: 'stock', element: withSuspense(<StockPage />) },
      { path: 'beneficiaries', element: withSuspense(<BeneficiariesPage />) },
      {
        path: 'donations',
        element: (
          <ProtectedRoute roles={['Admin', 'PointManager']}>
            {withSuspense(<DonationsPage />)}
          </ProtectedRoute>
        ),
      },
      {
        path: 'notices',
        element: (
          <ProtectedRoute roles={['Admin', 'PointManager']}>
            {withSuspense(<NoticesPage />)}
          </ProtectedRoute>
        ),
      },
      { path: 'withdrawals/new', element: withSuspense(<RegisterWithdrawalPage />) },
      {
        path: 'collection-points',
        element: <Navigate to="/app/admin/collection-points" replace />,
      },
      { path: 'users', element: <Navigate to="/app/admin/users" replace /> },
    ],
  },
  { path: '*', element: withSuspense(<NotFoundPage />), errorElement: <RouteError /> },
])
