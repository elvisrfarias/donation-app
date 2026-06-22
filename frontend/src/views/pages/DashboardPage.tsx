import { Building2, Church, Landmark, MapPin, PackageCheck, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import type { CollectionPointType } from '@/models/types'
import { useAuth, useCollectionPoints } from '@/viewmodels'
import { StatCard } from '@/views/components/common/StatCard'
import { Alert, Card, CenteredSpinner } from '@/views/components/ui'

const quickLinks = [
  {
    to: '/app/collection-points',
    label: 'Pontos de Coleta',
    description: 'Cadastrar e visualizar pontos.',
    icon: MapPin,
  },
  {
    to: '/app/families',
    label: 'Famílias',
    description: 'Beneficiários e histórico de retiradas.',
    icon: Users,
  },
  {
    to: '/app/withdrawals/new',
    label: 'Registrar Retirada',
    description: 'Aplica a regra de equidade municipal.',
    icon: PackageCheck,
  },
]

export function DashboardPage() {
  const { user } = useAuth()
  const { points, isLoading, isError, error } = useCollectionPoints()

  const byType = (type: CollectionPointType) => points.filter((p) => p.type === type).length
  const active = points.filter((p) => p.isActive).length

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">
          Olá, {user?.fullName?.split(' ')[0] ?? 'bem-vindo'} 👋
        </h1>
        <p className="mt-1 text-sm text-slate-500">Visão geral da rede de pontos de coleta.</p>
      </div>

      {isError && <Alert tone="error">{error?.displayMessage}</Alert>}

      {isLoading ? (
        <CenteredSpinner label="Carregando indicadores…" />
      ) : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard
              label="Pontos ativos"
              value={active}
              icon={<MapPin className="size-5" />}
              hint={`${points.length} no total`}
            />
            <StatCard
              label="Associações"
              value={byType('Association')}
              icon={<Building2 className="size-5" />}
            />
            <StatCard label="Igrejas" value={byType('Church')} icon={<Church className="size-5" />} />
            <StatCard
              label="Entidades públicas"
              value={byType('PublicEntity')}
              icon={<Landmark className="size-5" />}
            />
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            {quickLinks.map((link) => (
              <Link key={link.to} to={link.to}>
                <Card className="h-full p-5 transition-shadow hover:shadow-md">
                  <span className="grid size-10 place-items-center rounded-lg bg-brand-50 text-brand-600">
                    <link.icon className="size-5" />
                  </span>
                  <h3 className="mt-3 font-semibold text-slate-900">{link.label}</h3>
                  <p className="mt-1 text-sm text-slate-500">{link.description}</p>
                </Card>
              </Link>
            ))}
          </div>
        </>
      )}
    </div>
  )
}
