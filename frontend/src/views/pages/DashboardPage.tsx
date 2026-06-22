import { ArrowUpRight, Building2, Church, Landmark, MapPin, PackageCheck, Users } from 'lucide-react'
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
    <div className="mx-auto max-w-6xl space-y-8">
      <div>
        <p className="text-sm font-semibold text-brand-600">Painel</p>
        <h1 className="mt-1 text-3xl font-bold text-stone-900">
          Olá, {user?.fullName?.split(' ')[0] ?? 'bem-vindo'}
        </h1>
        <p className="mt-1.5 text-sm text-stone-500">Visão geral da rede de pontos de coleta.</p>
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

          <div>
            <h2 className="mb-3 text-sm font-semibold text-stone-500">Atalhos</h2>
            <div className="grid gap-4 md:grid-cols-3">
              {quickLinks.map((link) => (
                <Link key={link.to} to={link.to} className="group">
                  <Card className="flex h-full flex-col p-5 transition-shadow hover:shadow-lift">
                    <div className="flex items-start justify-between">
                      <span className="grid size-11 place-items-center rounded-xl bg-brand-50 text-brand-600 ring-1 ring-inset ring-brand-100 transition-colors group-hover:bg-brand-600 group-hover:text-white">
                        <link.icon className="size-5" />
                      </span>
                      <ArrowUpRight className="size-5 text-stone-300 transition-all group-hover:translate-x-0.5 group-hover:-translate-y-0.5 group-hover:text-brand-600" />
                    </div>
                    <h3 className="mt-4 font-semibold text-stone-900">{link.label}</h3>
                    <p className="mt-1 text-sm text-stone-500">{link.description}</p>
                  </Card>
                </Link>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  )
}
