import { ArrowUpRight, Building2, Church, Landmark, MapPin, PackageCheck, Users } from 'lucide-react'
import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  XAxis,
  YAxis,
} from 'recharts'
import {
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from '@/components/ui/chart'
import { collectionPointTypeLabels, type CollectionPoint, type CollectionPointType } from '@/models/types'
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

// Cores por tipo alinhadas à paleta dos charts (e aos marcadores do mapa).
const growthConfig: ChartConfig = {
  total: { label: 'Total de pontos', color: 'var(--chart-1)' },
}

const typeConfig: ChartConfig = {
  value: { label: 'Pontos' },
  Association: { label: collectionPointTypeLabels.Association, color: 'var(--chart-1)' },
  Church: { label: collectionPointTypeLabels.Church, color: 'var(--chart-2)' },
  PublicEntity: { label: collectionPointTypeLabels.PublicEntity, color: 'var(--chart-4)' },
}

const cityConfig: ChartConfig = {
  count: { label: 'Pontos', color: 'var(--chart-1)' },
}

const monthFormatter = new Intl.DateTimeFormat('pt-BR', { month: 'short', year: '2-digit' })

function buildMonthlyGrowth(points: CollectionPoint[]) {
  if (!points.length) return []
  const buckets = new Map<string, { date: number; label: string; novos: number }>()
  for (const p of points) {
    const d = new Date(p.createdAtUtc)
    const key = `${d.getFullYear()}-${d.getMonth()}`
    const monthStart = new Date(d.getFullYear(), d.getMonth(), 1)
    const bucket = buckets.get(key) ?? {
      date: monthStart.getTime(),
      label: monthFormatter.format(monthStart),
      novos: 0,
    }
    bucket.novos += 1
    buckets.set(key, bucket)
  }
  const sorted = [...buckets.values()].sort((a, b) => a.date - b.date)
  let running = 0
  return sorted
    .map((b) => {
      running += b.novos
      return { month: b.label, total: running }
    })
    .slice(-6)
}

export function DashboardPage() {
  const { user } = useAuth()
  const { points, isLoading, isError, error } = useCollectionPoints()

  const byType = (type: CollectionPointType) => points.filter((p) => p.type === type).length
  const active = points.filter((p) => p.isActive).length

  const growthData = useMemo(() => buildMonthlyGrowth(points), [points])

  const typeData = useMemo(
    () =>
      (['Association', 'Church', 'PublicEntity'] as CollectionPointType[])
        .map((type) => ({
          type,
          label: collectionPointTypeLabels[type],
          value: byType(type),
          fill: `var(--color-${type})`,
        }))
        .filter((d) => d.value > 0),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [points],
  )

  const cityData = useMemo(() => {
    const counts = new Map<string, number>()
    for (const p of points) counts.set(p.address.city, (counts.get(p.address.city) ?? 0) + 1)
    return [...counts.entries()]
      .map(([city, count]) => ({ city, count }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 6)
  }, [points])

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

          {/* Gráficos */}
          <div className="grid gap-4 lg:grid-cols-2">
            {/* Crescimento da rede (área) */}
            <Card className="p-6 lg:col-span-2">
              <h2 className="text-base font-semibold text-stone-900">Crescimento da rede</h2>
              <p className="mt-1 text-sm text-stone-500">
                Total acumulado de pontos cadastrados por mês.
              </p>
              <ChartContainer config={growthConfig} className="mt-4 aspect-auto h-65 w-full">
                <AreaChart data={growthData} margin={{ left: 4, right: 12, top: 8 }}>
                  <defs>
                    <linearGradient id="fillTotal" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="var(--color-total)" stopOpacity={0.3} />
                      <stop offset="95%" stopColor="var(--color-total)" stopOpacity={0.02} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid vertical={false} />
                  <XAxis
                    dataKey="month"
                    tickLine={false}
                    axisLine={false}
                    tickMargin={8}
                    minTickGap={16}
                  />
                  <YAxis tickLine={false} axisLine={false} width={28} allowDecimals={false} />
                  <ChartTooltip cursor={false} content={<ChartTooltipContent indicator="line" />} />
                  <Area
                    dataKey="total"
                    type="monotone"
                    fill="url(#fillTotal)"
                    stroke="var(--color-total)"
                    strokeWidth={2}
                  />
                </AreaChart>
              </ChartContainer>
            </Card>

            {/* Distribuição por tipo (donut) */}
            <Card className="p-6">
              <h2 className="text-base font-semibold text-stone-900">Distribuição por tipo</h2>
              <p className="mt-1 text-sm text-stone-500">Pontos cadastrados por categoria.</p>
              <ChartContainer
                config={typeConfig}
                className="mx-auto mt-4 aspect-square h-65"
              >
                <PieChart>
                  <ChartTooltip
                    cursor={false}
                    content={<ChartTooltipContent nameKey="type" hideLabel />}
                  />
                  <Pie data={typeData} dataKey="value" nameKey="type" innerRadius={55} strokeWidth={4}>
                    {typeData.map((entry) => (
                      <Cell key={entry.type} fill={entry.fill} />
                    ))}
                  </Pie>
                  <ChartLegend content={<ChartLegendContent nameKey="type" />} />
                </PieChart>
              </ChartContainer>
            </Card>

            {/* Pontos por cidade (barras) */}
            <Card className="p-6">
              <h2 className="text-base font-semibold text-stone-900">Pontos por cidade</h2>
              <p className="mt-1 text-sm text-stone-500">Principais municípios da rede.</p>
              <ChartContainer config={cityConfig} className="mt-4 aspect-auto h-65 w-full">
                <BarChart data={cityData} layout="vertical" margin={{ left: 8, right: 16 }}>
                  <CartesianGrid horizontal={false} />
                  <XAxis type="number" hide allowDecimals={false} />
                  <YAxis
                    type="category"
                    dataKey="city"
                    tickLine={false}
                    axisLine={false}
                    width={96}
                  />
                  <ChartTooltip cursor={false} content={<ChartTooltipContent hideLabel />} />
                  <Bar dataKey="count" fill="var(--color-count)" radius={[0, 6, 6, 0]} />
                </BarChart>
              </ChartContainer>
            </Card>
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
