import { LayoutDashboard, LogOut, Map, MapPin, PackageCheck, Users } from 'lucide-react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { cn, initials } from '@/lib/utils'
import { roleLabels } from '@/models/types/auth'
import { useAuth } from '@/viewmodels'
import { AppLogo } from '@/views/components/common/AppLogo'
import { Button } from '@/views/components/ui'

const navItems = [
  { to: '/app', label: 'Painel', icon: LayoutDashboard, end: true },
  { to: '/app/collection-points', label: 'Pontos de Coleta', icon: MapPin, end: false },
  { to: '/app/families', label: 'Famílias', icon: Users, end: false },
  { to: '/app/withdrawals/new', label: 'Registrar Retirada', icon: PackageCheck, end: false },
]

export function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  return (
    <div className="flex min-h-dvh bg-slate-50">
      <aside className="hidden w-64 shrink-0 flex-col border-r border-slate-200 bg-white px-4 py-5 lg:flex">
        <AppLogo className="px-2" />

        <nav className="mt-8 flex flex-1 flex-col gap-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                  isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100',
                )
              }
            >
              <item.icon className="size-5" />
              {item.label}
            </NavLink>
          ))}

          <NavLink
            to="/"
            className="mt-1 flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
          >
            <Map className="size-5" />
            Mapa público
          </NavLink>
        </nav>

        <div className="mt-auto rounded-lg bg-slate-50 p-3">
          <div className="flex items-center gap-2">
            <span className="grid size-9 shrink-0 place-items-center rounded-full bg-brand-600 text-sm font-semibold text-white">
              {initials(user?.fullName ?? '?')}
            </span>
            <div className="min-w-0">
              <p className="truncate text-sm font-medium text-slate-800">{user?.fullName}</p>
              <p className="truncate text-xs text-slate-500">
                {user?.roles.map((r) => roleLabels[r]).join(', ')}
              </p>
            </div>
          </div>
          <Button
            variant="secondary"
            size="sm"
            className="mt-3 w-full"
            leftIcon={<LogOut className="size-4" />}
            onClick={handleLogout}
          >
            Sair
          </Button>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-slate-200 bg-white px-4 py-3 lg:hidden">
          <AppLogo compact />
          <Button
            variant="ghost"
            size="sm"
            leftIcon={<LogOut className="size-4" />}
            onClick={handleLogout}
          >
            Sair
          </Button>
        </header>

        <main className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
