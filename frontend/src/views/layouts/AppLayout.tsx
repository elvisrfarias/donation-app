import { useState } from 'react'
import {
  Boxes,
  ChevronDown,
  LayoutDashboard,
  LogOut,
  Map,
  MapPin,
  PackageCheck,
  Shield,
  ShieldUser,
  UserPlus,
} from 'lucide-react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { cn, initials } from '@/lib/utils'
import { roleLabels } from '@/models/types/auth'
import { useAuth } from '@/viewmodels'
import { AppLogo } from '@/views/components/common/AppLogo'

interface NavItem {
  to: string
  label: string
  icon: typeof LayoutDashboard
  end: boolean
}

const ADMIN_ROUTE_PREFIX = '/app/admin'

const mainNavItems: NavItem[] = [
  { to: '/app', label: 'Painel', icon: LayoutDashboard, end: true },
  { to: '/app/stock', label: 'Estoque', icon: Boxes, end: false },
  { to: '/app/beneficiaries', label: 'Beneficiários', icon: UserPlus, end: false },
  { to: '/app/withdrawals/new', label: 'Registrar Retirada', icon: PackageCheck, end: false },
]

const collectionPointsNavItem: NavItem = {
  to: `${ADMIN_ROUTE_PREFIX}/collection-points`,
  label: 'Pontos de Coleta',
  icon: MapPin,
  end: false,
}

const usersNavItem: NavItem = {
  to: `${ADMIN_ROUTE_PREFIX}/users`,
  label: 'Usuários',
  icon: ShieldUser,
  end: false,
}

const adminMobileNavItem: NavItem = {
  to: ADMIN_ROUTE_PREFIX,
  label: 'Admin',
  icon: Shield,
  end: false,
}

function SidebarLink({ item, nested = false }: { item: NavItem; nested?: boolean }) {
  return (
    <NavLink
      to={item.to}
      end={item.end}
      className={({ isActive }) =>
        cn(
          'group flex items-center gap-3 rounded-2xl px-3.5 py-2.5 text-sm font-semibold transition-all',
          nested && 'py-2 pl-4',
          isActive
            ? 'text-brand-700 shadow-lift bg-white'
            : 'text-white/65 hover:bg-white/10 hover:text-white',
        )
      }
    >
      {({ isActive }) => (
        <>
          <item.icon
            className={cn(
              nested ? 'size-4' : 'size-5',
              'transition-colors',
              isActive ? 'text-brand-600' : 'text-white/55 group-hover:text-white',
            )}
          />
          {item.label}
        </>
      )}
    </NavLink>
  )
}

function AdminMenu({ items }: { items: NavItem[] }) {
  const { pathname } = useLocation()
  const isOnAdminRoute = pathname.startsWith(ADMIN_ROUTE_PREFIX)
  const [isOpen, setIsOpen] = useState(isOnAdminRoute)
  const isExpanded = isOpen || isOnAdminRoute

  return (
    <div className="flex flex-col gap-1.5">
      <button
        type="button"
        aria-expanded={isExpanded}
        onClick={() => setIsOpen(!isExpanded)}
        className={cn(
          'group flex items-center gap-3 rounded-2xl px-3.5 py-2.5 text-sm font-semibold transition-all',
          isOnAdminRoute ? 'text-white' : 'text-white/65 hover:bg-white/10 hover:text-white',
        )}
      >
        <Shield className="size-5 text-white/55 group-hover:text-white" />
        Admin
        <ChevronDown
          className={cn('ml-auto size-4 transition-transform', isExpanded && 'rotate-180')}
        />
      </button>
      {isExpanded && (
        <div className="ml-3 flex flex-col gap-1 border-l border-white/15 pl-2">
          {items.map((item) => (
            <SidebarLink key={item.to} item={item} nested />
          ))}
        </div>
      )}
    </div>
  )
}

export function AppLayout() {
  const { user, logout, isAdmin, isManager } = useAuth()
  const navigate = useNavigate()
  const adminNavItems: NavItem[] = isAdmin
    ? [collectionPointsNavItem, usersNavItem]
    : [usersNavItem]
  const mobileNavItems: NavItem[] = isManager ? [...mainNavItems, adminMobileNavItem] : mainNavItems

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  return (
    <div className="bg-canvas flex h-dvh overflow-hidden">
      <aside className="from-sidebar to-sidebar-deep hidden w-64 shrink-0 flex-col overflow-y-auto bg-linear-to-b px-4 py-6 lg:flex">
        <AppLogo className="px-1" inverted />

        <nav className="mt-9 flex flex-1 flex-col gap-1.5">
          {mainNavItems.map((item) => (
            <SidebarLink key={item.to} item={item} />
          ))}
          {isManager && <AdminMenu items={adminNavItems} />}

          <NavLink
            to="/"
            className="group mt-1 flex items-center gap-3 rounded-2xl px-3.5 py-2.5 text-sm font-semibold text-white/65 transition-all hover:bg-white/10 hover:text-white"
          >
            <Map className="size-5 text-white/55 group-hover:text-white" />
            Mapa público
          </NavLink>
        </nav>

        <div className="mt-auto rounded-3xl bg-white/10 p-3 ring-1 ring-white/15 backdrop-blur-sm ring-inset">
          <div className="flex items-center gap-2.5">
            <span className="text-brand-700 grid size-9 shrink-0 place-items-center rounded-full bg-white text-sm font-bold">
              {initials(user?.fullName ?? '?')}
            </span>
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold text-white">{user?.fullName}</p>
              <p className="truncate text-xs text-white/60">
                {user?.roles.map((r) => roleLabels[r]).join(', ')}
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={handleLogout}
            className="hover:text-brand-700 mt-3 flex w-full items-center justify-center gap-2 rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/85 ring-1 ring-white/15 transition-colors ring-inset hover:bg-white hover:ring-white"
          >
            <LogOut className="size-4" />
            Sair
          </button>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
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

        <main className="min-h-0 flex-1 overflow-y-auto p-4 pb-24 sm:p-6 lg:p-8 lg:pb-8">
          <Outlet />
        </main>
      </div>

      <nav className="fixed inset-x-0 bottom-0 z-20 flex items-stretch border-t border-stone-200 bg-white/90 backdrop-blur-md lg:hidden">
        {mobileNavItems.map((item) => (
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
