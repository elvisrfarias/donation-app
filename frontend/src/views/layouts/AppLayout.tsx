import { LayoutDashboard, LogOut, Map, MapPin, PackageCheck, Users } from 'lucide-react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { cn, initials } from '@/lib/utils'
import { roleLabels } from '@/models/types/auth'
import { useAuth } from '@/viewmodels'
import { AppLogo } from '@/views/components/common/AppLogo'

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
    <div className="flex min-h-dvh bg-stone-50">
      {/* Sidebar (desktop) */}
      <aside className="hidden w-64 shrink-0 flex-col bg-ink-950 px-3 py-5 lg:flex">
        <AppLogo className="px-2" inverted />

        <nav className="mt-8 flex flex-1 flex-col gap-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                cn(
                  'group relative flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-white/10 text-white'
                    : 'text-stone-400 hover:bg-white/5 hover:text-stone-100',
                )
              }
            >
              {({ isActive }) => (
                <>
                  <span
                    className={cn(
                      'absolute left-0 h-5 w-1 rounded-r-full bg-brand-500 transition-opacity',
                      isActive ? 'opacity-100' : 'opacity-0',
                    )}
                  />
                  <item.icon
                    className={cn('size-5', isActive ? 'text-brand-400' : 'text-stone-500 group-hover:text-stone-300')}
                  />
                  {item.label}
                </>
              )}
            </NavLink>
          ))}

          <NavLink
            to="/"
            className="group mt-1 flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-stone-400 transition-colors hover:bg-white/5 hover:text-stone-100"
          >
            <Map className="size-5 text-stone-500 group-hover:text-stone-300" />
            Mapa público
          </NavLink>
        </nav>

        {/* Cartão do usuário */}
        <div className="mt-auto rounded-2xl bg-white/5 p-3 ring-1 ring-inset ring-white/10">
          <div className="flex items-center gap-2.5">
            <span className="grid size-9 shrink-0 place-items-center rounded-full bg-linear-to-br from-brand-400 to-brand-600 text-sm font-bold text-white">
              {initials(user?.fullName ?? '?')}
            </span>
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold text-white">{user?.fullName}</p>
              <p className="truncate text-xs text-stone-400">
                {user?.roles.map((r) => roleLabels[r]).join(', ')}
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={handleLogout}
            className="mt-3 flex w-full items-center justify-center gap-2 rounded-xl bg-white/5 px-4 py-2 text-sm font-semibold text-stone-300 ring-1 ring-inset ring-white/10 transition-colors hover:bg-white/10 hover:text-white"
          >
            <LogOut className="size-4" />
            Sair
          </button>
        </div>
      </aside>

      {/* Conteúdo */}
      <div className="flex min-w-0 flex-1 flex-col">
        {/* Topbar (mobile) */}
        <header className="sticky top-0 z-20 flex items-center justify-between border-b border-stone-200 bg-white/80 px-4 py-3 backdrop-blur-md lg:hidden">
          <AppLogo compact />
          <button
            type="button"
            onClick={handleLogout}
            className="inline-flex h-9 items-center gap-2 rounded-xl px-3 text-sm font-semibold text-stone-600 transition-colors hover:bg-stone-100"
          >
            <LogOut className="size-4" />
            Sair
          </button>
        </header>

        <main className="flex-1 overflow-y-auto p-4 pb-24 sm:p-6 lg:p-8 lg:pb-8">
          <Outlet />
        </main>
      </div>

      {/* Bottom nav (mobile) */}
      <nav className="fixed inset-x-0 bottom-0 z-20 flex items-stretch border-t border-stone-200 bg-white/90 backdrop-blur-md lg:hidden">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) =>
              cn(
                'flex flex-1 flex-col items-center justify-center gap-1 py-2.5 text-[11px] font-medium transition-colors',
                isActive ? 'text-brand-600' : 'text-stone-400',
              )
            }
          >
            <item.icon className="size-5" />
            <span className="max-w-full truncate px-1">{item.label.split(' ')[0]}</span>
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
