import {
  ArrowUpRight,
  Boxes,
  ClipboardList,
  HeartHandshake,
  PackageCheck,
  Star,
  Users,
} from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from 'recharts'
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from '@/components/ui/chart'
import { currentMonthPeriod, periodError } from '@/lib/dashboardPeriod'
import { isoToBrazilian, isoToDate } from '@/lib/dateInput'
import {
  cycleTypeLabels,
  type CollectionPoint,
  type CollectionPointDashboard,
  type DashboardGranularity,
  type StockItem,
  type StockItemStatus,
  type WithdrawalsSeriesPoint,
} from '@/models/types'
import { useCollectionPoint, useCollectionPointDashboard } from '@/viewmodels'
import { StatCard } from '@/views/components/common/StatCard'
import { WithdrawalItemBadges } from '@/views/components/withdrawal/WithdrawalItemBadge'
import { DashboardHeader } from './DashboardHeader'
import { PeriodFilter } from './PeriodFilter'
import { itemsNeedingRestock } from '@/views/components/stock/stockDraft'
import { Alert, Badge, Card, CenteredSpinner, EmptyState } from '@/views/components/ui'

const withdrawalsConfig: ChartConfig = {
  count: { label: 'Retiradas', color: 'var(--chart-1)' },
}

const stockStatusPresentation: Record<
  StockItemStatus,
  { label: string; tone: 'red' | 'amber' | 'green' | 'brand' }
> = {
  out: { label: 'Em falta', tone: 'red' },
  low: { label: 'Estoque baixo', tone: 'amber' },
  ok: { label: 'Disponível', tone: 'green' },
  goal: { label: 'Meta atingida', tone: 'brand' },
}

const monthFormatter = new Intl.DateTimeFormat('pt-BR', { month: 'short', year: '2-digit' })
const dateTimeFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

function toChartData(series: WithdrawalsSeriesPoint[], granularity: DashboardGranularity) {
  return series.map(({ date, count }) => {
    const day = isoToDate(date) ?? new Date(date)
    const isDaily = granularity === 'Day'
    return {
      label: isDaily ? String(day.getDate()) : monthFormatter.format(day).replace('.', ''),
      fullLabel: isDaily ? isoToBrazilian(date) : monthFormatter.format(day),
      count,
    }
  })
}

function describePeriod(dashboard: CollectionPointDashboard): string {
  return `${isoToBrazilian(dashboard.periodStart)} a ${isoToBrazilian(dashboard.periodEnd)}`
}

function describeWithdrawalRule(point: CollectionPoint): string {
  const { maxWithdrawalsPerCycle, type } = point.withdrawalRule
  const times = maxWithdrawalsPerCycle === 1 ? 'retirada' : 'retiradas'
  return `${maxWithdrawalsPerCycle} ${times} por família · ciclo ${cycleTypeLabels[type].toLowerCase()}`
}

function describeRestockHint(totalItems: number): string {
  if (totalItems === 0) return 'nenhum item no catálogo'
  return `de ${totalItems} ${totalItems === 1 ? 'item no catálogo' : 'itens no catálogo'}`
}

function PointSummary({
  point,
  dashboard,
}: {
  point: CollectionPoint
  dashboard: CollectionPointDashboard
}) {
  const restockCount = itemsNeedingRestock(point.stockItems).length
  const rating = point.reviewCount > 0 ? point.averageRating.toFixed(1) : '—'

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard
        gradient
        accent="brand"
        label="Retiradas no período"
        value={dashboard.withdrawalsInPeriod}
        icon={<PackageCheck className="size-5" />}
        hint={`${dashboard.totalWithdrawals} desde o início do ponto`}
      />
      <StatCard
        gradient
        accent="green"
        label="Beneficiários atendidos"
        value={dashboard.beneficiariesServedInPeriod}
        icon={<HeartHandshake className="size-5" />}
        hint={`no período · ${dashboard.registeredBeneficiaries} cadastrados por este ponto`}
      />
      <StatCard
        gradient
        accent="amber"
        label="Itens para repor"
        value={restockCount}
        icon={<Boxes className="size-5" />}
        hint={`${describeRestockHint(point.stockItems.length)} · situação atual`}
      />
      <StatCard
        gradient
        accent="blue"
        label="Avaliação do ponto"
        value={rating}
        icon={<Star className="size-5" />}
        hint={point.reviewCount > 0 ? `${point.reviewCount} avaliações` : 'ainda sem avaliações'}
      />
    </div>
  )
}

function WithdrawalsSeriesCard({ dashboard }: { dashboard: CollectionPointDashboard }) {
  const isDaily = dashboard.granularity === 'Day'

  return (
    <Card className="p-6 lg:col-span-2">
      <h2 className="text-base font-semibold text-stone-900">
        {isDaily ? 'Retiradas por dia' : 'Retiradas por mês'}
      </h2>
      <p className="mt-1 text-sm text-stone-500">{describePeriod(dashboard)}, só deste ponto.</p>
      <ChartContainer config={withdrawalsConfig} className="mt-4 aspect-auto h-52 w-full">
        <BarChart
          data={toChartData(dashboard.withdrawalsSeries, dashboard.granularity)}
          margin={{ left: 4, right: 12, top: 8 }}
        >
          <CartesianGrid vertical={false} />
          <XAxis dataKey="label" tickLine={false} axisLine={false} tickMargin={8} minTickGap={6} />
          <YAxis tickLine={false} axisLine={false} width={28} allowDecimals={false} />
          <ChartTooltip
            cursor={false}
            content={
              <ChartTooltipContent
                labelFormatter={(_, payload) => String(payload?.[0]?.payload?.fullLabel ?? '')}
              />
            }
          />
          <Bar dataKey="count" fill="var(--color-count)" radius={[6, 6, 0, 0]} />
        </BarChart>
      </ChartContainer>
    </Card>
  )
}

const MAX_RESTOCK_ITEMS_SHOWN = 6

function StockOverviewCard({ point }: { point: CollectionPoint }) {
  const needingRestock = itemsNeedingRestock(point.stockItems)
  const shown = needingRestock.slice(0, MAX_RESTOCK_ITEMS_SHOWN)
  const hiddenCount = needingRestock.length - shown.length

  return (
    <Card className="p-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-base font-semibold text-stone-900">Situação do estoque</h2>
          <p className="mt-1 text-sm text-stone-500">Itens em falta ou com estoque baixo.</p>
        </div>
        <Link to="/app/stock" className="text-brand-700 text-sm font-semibold hover:underline">
          Gerenciar
        </Link>
      </div>
      {shown.length === 0 ? (
        <p className="mt-4 text-sm text-stone-500">Nenhum item precisa de reposição agora.</p>
      ) : (
        <ul className="mt-4 divide-y divide-stone-100">
          {shown.map((item: StockItem) => {
            const presentation = stockStatusPresentation[item.status]
            return (
              <li key={item.id} className="flex items-center justify-between gap-3 py-2.5">
                <span className="min-w-0 truncate text-sm font-medium text-stone-800">
                  {item.name}
                </span>
                <span className="flex shrink-0 items-center gap-3">
                  <span className="text-sm font-semibold text-stone-900 tabular-nums">
                    {item.quantity}
                  </span>
                  <Badge tone={presentation.tone}>{presentation.label}</Badge>
                </span>
              </li>
            )
          })}
        </ul>
      )}
      {hiddenCount > 0 && (
        <p className="mt-2 text-xs text-stone-500">e mais {hiddenCount} itens…</p>
      )}
      <p className="mt-4 border-t border-stone-100 pt-3 text-xs text-stone-500">
        Regra de retirada: {describeWithdrawalRule(point)}
      </p>
    </Card>
  )
}

function RecentWithdrawalsCard({ dashboard }: { dashboard: CollectionPointDashboard }) {
  return (
    <Card className="p-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-base font-semibold text-stone-900">Últimas retiradas</h2>
          <p className="mt-1 text-sm text-stone-500">Registradas neste ponto, no período.</p>
        </div>
        <Link
          to="/app/withdrawals/new"
          className="text-brand-700 text-sm font-semibold hover:underline"
        >
          Registrar
        </Link>
      </div>
      {dashboard.recentWithdrawals.length === 0 ? (
        <EmptyState
          icon={<ClipboardList className="size-6" />}
          title="Nenhuma retirada no período"
          description="Ajuste as datas ou registre uma retirada."
        />
      ) : (
        <ul className="mt-4 divide-y divide-stone-100">
          {dashboard.recentWithdrawals.map((withdrawal) => (
            <li
              key={withdrawal.withdrawalId}
              className="flex items-center justify-between gap-3 py-2.5"
            >
              <span className="flex min-w-0 items-center gap-2">
                <span className="truncate text-sm font-medium text-stone-800">
                  {withdrawal.beneficiaryName}
                </span>
                <WithdrawalItemBadges items={withdrawal.items} />
              </span>
              <time
                dateTime={withdrawal.occurredAtUtc}
                className="shrink-0 text-xs text-stone-500 tabular-nums"
              >
                {dateTimeFormatter.format(new Date(withdrawal.occurredAtUtc))}
              </time>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}

const pointQuickLinks = [
  {
    to: '/app/withdrawals/new',
    label: 'Registrar Retirada',
    description: 'Confere o limite e registra a retirada.',
    icon: PackageCheck,
  },
  {
    to: '/app/stock',
    label: 'Estoque',
    description: 'Atualize quantidades, metas e avisos.',
    icon: Boxes,
  },
  {
    to: '/app/beneficiaries',
    label: 'Beneficiários',
    description: 'Cadastre e edite quem o ponto atende.',
    icon: Users,
  },
]

function PointQuickLinks() {
  return (
    <div>
      <h2 className="mb-3 text-sm font-semibold text-stone-500">Atalhos</h2>
      <div className="grid gap-4 md:grid-cols-3">
        {pointQuickLinks.map((link) => (
          <Link key={link.to} to={link.to} className="group">
            <Card className="hover:shadow-lift flex h-full flex-col p-5 transition-shadow">
              <div className="flex items-start justify-between">
                <span className="bg-brand-50 text-brand-600 ring-brand-100 group-hover:bg-brand-600 grid size-11 place-items-center rounded-xl ring-1 transition-colors ring-inset group-hover:text-white">
                  <link.icon className="size-5" />
                </span>
                <ArrowUpRight className="group-hover:text-brand-600 size-5 text-stone-300 transition-all group-hover:translate-x-0.5 group-hover:-translate-y-0.5" />
              </div>
              <h3 className="mt-4 font-semibold text-stone-900">{link.label}</h3>
              <p className="mt-1 text-sm text-stone-500">{link.description}</p>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  )
}

function describeSubtitle(point: CollectionPoint | null): string {
  return point
    ? `${point.name} · ${point.address.city}/${point.address.state}`
    : 'Resultados e situação do seu ponto de coleta.'
}

export function PointDashboard({ pointId, title }: { pointId: string | null; title: string }) {
  const [period, setPeriod] = useState(currentMonthPeriod)
  const pointQuery = useCollectionPoint(pointId)
  const dashboardQuery = useCollectionPointDashboard(pointId, period, periodError(period) === null)
  const { point } = pointQuery
  const { dashboard } = dashboardQuery

  return (
    <div className="space-y-5">
      <DashboardHeader
        title={title}
        subtitle={describeSubtitle(point)}
        actions={pointId && <PeriodFilter period={period} onChange={setPeriod} />}
      />
      <PointDashboardBody
        pointId={pointId}
        point={point}
        dashboard={dashboard}
        isLoading={pointQuery.isLoading || dashboardQuery.isLoading}
        errorMessage={(pointQuery.error ?? dashboardQuery.error)?.displayMessage}
      />
    </div>
  )
}

interface PointDashboardBodyProps {
  pointId: string | null
  point: CollectionPoint | null
  dashboard: CollectionPointDashboard | null
  isLoading: boolean
  errorMessage?: string
}

function PointDashboardBody({
  pointId,
  point,
  dashboard,
  isLoading,
  errorMessage,
}: PointDashboardBodyProps) {
  if (!pointId) {
    return (
      <Alert tone="warning" title="Seu usuário não está vinculado a um ponto">
        Peça a um administrador para vincular você a um ponto de coleta.
      </Alert>
    )
  }

  if (isLoading) return <CenteredSpinner label="Carregando indicadores…" />

  if (!point || !dashboard) {
    return (
      <Alert tone="error" title="Não foi possível carregar o painel do ponto">
        {errorMessage}
      </Alert>
    )
  }

  return (
    <div className="space-y-6">
      <PointSummary point={point} dashboard={dashboard} />
      <div className="grid gap-4 lg:grid-cols-2">
        <WithdrawalsSeriesCard dashboard={dashboard} />
        <StockOverviewCard point={point} />
        <RecentWithdrawalsCard dashboard={dashboard} />
      </div>
      <PointQuickLinks />
    </div>
  )
}
