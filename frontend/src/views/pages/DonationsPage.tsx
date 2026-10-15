import { useState } from 'react'
import { HandHeart, Hourglass, PackageCheck, Phone, XCircle } from 'lucide-react'
import { formatRelativeTime } from '@/lib/relativeTime'
import { formatDateTime, initials } from '@/lib/utils'
import { donationStatusLabels, type Donation, type DonationStatus } from '@/models/types'
import {
  useAuth,
  useCancelDonation,
  useCollectionPointsForManagement,
  usePointDonations,
} from '@/viewmodels'
import { WhatsAppLink } from '@/views/components/beneficiaries/WhatsAppLink'
import { StatCard } from '@/views/components/common/StatCard'
import { HowItWorks } from '@/views/components/common/HowItWorks'
import { ReceiveDonationModal } from '@/views/components/donations/ReceiveDonationModal'
import {
  Alert,
  Badge,
  Button,
  Card,
  CenteredSpinner,
  EmptyState,
  SectionTitle,
  Select,
  TabPanel,
  Tabs,
} from '@/views/components/ui'

const statusTone: Record<DonationStatus, 'amber' | 'green' | 'slate'> = {
  Pledged: 'amber',
  Received: 'green',
  Cancelled: 'slate',
}

const statusTabs: { value: DonationStatus; label: string }[] = [
  { value: 'Pledged', label: 'Aguardando' },
  { value: 'Received', label: 'Recebidas' },
  { value: 'Cancelled', label: 'Canceladas' },
]

const emptyMessages: Record<DonationStatus, { title: string; description: string }> = {
  Pledged: {
    title: 'Nenhuma doação aguardando entrega',
    description: 'Quando alguém avisar pelo mapa público que vai doar, aparece aqui.',
  },
  Received: {
    title: 'Nenhuma doação recebida ainda',
    description: 'As doações que você confirmar ficam registradas aqui.',
  },
  Cancelled: {
    title: 'Nenhuma doação cancelada',
    description: 'As doações canceladas aparecem aqui.',
  },
}

function DonationRow({
  donation,
  pointId,
  onReceive,
}: {
  donation: Donation
  pointId: string
  onReceive: (donation: Donation) => void
}) {
  const cancel = useCancelDonation(pointId)
  const [confirmingCancel, setConfirmingCancel] = useState(false)
  const isPending = donation.status === 'Pledged'

  return (
    <li className="flex flex-col gap-3 py-4 sm:flex-row sm:items-center">
      <span
        aria-hidden
        className="bg-brand-50 text-brand-700 hidden size-11 shrink-0 place-items-center rounded-full text-sm font-bold sm:grid"
      >
        {initials(donation.donorName)}
      </span>

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <p className="font-semibold text-stone-900">{donation.donorName}</p>
          <Badge tone={statusTone[donation.status]}>{donationStatusLabels[donation.status]}</Badge>
        </div>
        <p className="mt-1 text-sm text-stone-700">{donation.description}</p>
        <p className="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-stone-500">
          <time dateTime={donation.createdAtUtc} title={formatDateTime(donation.createdAtUtc)}>
            {formatRelativeTime(donation.createdAtUtc)}
          </time>
          {donation.donorPhone && (
            <span className="inline-flex items-center gap-1.5">
              <Phone className="size-3" aria-hidden />
              {donation.donorPhone}
              <WhatsAppLink phone={donation.donorPhone} personName={donation.donorName} />
            </span>
          )}
        </p>
        {cancel.error && (
          <Alert tone="error" className="mt-2">
            {cancel.error.displayMessage}
          </Alert>
        )}
      </div>

      {isPending && (
        <div className="flex shrink-0 flex-wrap gap-2">
          <Button size="sm" variant="brand" onClick={() => onReceive(donation)}>
            Recebi
          </Button>
          {confirmingCancel ? (
            <>
              <Button
                size="sm"
                variant="danger"
                loading={cancel.isPending}
                onClick={() => cancel.mutate(donation.id)}
              >
                Confirmar cancelamento
              </Button>
              <Button size="sm" variant="ghost" onClick={() => setConfirmingCancel(false)}>
                Voltar
              </Button>
            </>
          ) : (
            <Button
              size="sm"
              variant="secondary"
              leftIcon={<XCircle className="size-4" />}
              onClick={() => setConfirmingCancel(true)}
            >
              Cancelar
            </Button>
          )}
        </div>
      )}
    </li>
  )
}

function countByStatus(donations: Donation[]): Record<DonationStatus, number> {
  return {
    Pledged: donations.filter((donation) => donation.status === 'Pledged').length,
    Received: donations.filter((donation) => donation.status === 'Received').length,
    Cancelled: donations.filter((donation) => donation.status === 'Cancelled').length,
  }
}

function DonationsList({ pointId }: { pointId: string }) {
  const { donations, isLoading, isError, error } = usePointDonations(pointId)
  const [receiving, setReceiving] = useState<Donation | null>(null)
  const [activeStatus, setActiveStatus] = useState<DonationStatus>('Pledged')

  if (isLoading) return <CenteredSpinner label="Carregando doações…" />
  if (isError) return <Alert tone="error">{error?.displayMessage}</Alert>

  const counts = countByStatus(donations)
  const visible = donations.filter((donation) => donation.status === activeStatus)
  const empty = emptyMessages[activeStatus]

  return (
    <>
      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard
          gradient
          accent="amber"
          label="Aguardando entrega"
          value={counts.Pledged}
          icon={<Hourglass className="size-5" />}
        />
        <StatCard
          gradient
          accent="green"
          label="Recebidas"
          value={counts.Received}
          icon={<PackageCheck className="size-5" />}
        />
        <StatCard
          accent="brand"
          label="Canceladas"
          value={counts.Cancelled}
          icon={<XCircle className="size-5" />}
        />
      </div>

      <Tabs
        ariaLabel="Situação das doações"
        value={activeStatus}
        onValueChange={(value) => setActiveStatus(value as DonationStatus)}
        tabs={statusTabs.map((tab) => ({
          ...tab,
          badge: tab.value === 'Pledged' ? counts.Pledged : undefined,
        }))}
      />

      <TabPanel value={activeStatus} activeValue={activeStatus}>
        {visible.length === 0 ? (
          <EmptyState
            icon={<HandHeart className="size-6" />}
            title={empty.title}
            description={empty.description}
          />
        ) : (
          <Card className="px-6">
            <ul className="divide-y divide-stone-100" aria-live="polite">
              {visible.map((donation) => (
                <DonationRow
                  key={donation.id}
                  donation={donation}
                  pointId={pointId}
                  onReceive={setReceiving}
                />
              ))}
            </ul>
          </Card>
        )}
      </TabPanel>

      <ReceiveDonationModal
        pointId={pointId}
        donation={receiving}
        onClose={() => setReceiving(null)}
      />
    </>
  )
}

function AdminDonations() {
  const { points, isLoading } = useCollectionPointsForManagement()
  const [chosenId, setChosenId] = useState('')

  return (
    <>
      <Card className="space-y-2 p-5">
        <label htmlFor="donations-point" className="text-sm font-semibold text-stone-700">
          Ponto de coleta
        </label>
        <Select
          id="donations-point"
          value={chosenId}
          onValueChange={setChosenId}
          disabled={isLoading}
        >
          <option value="">{isLoading ? 'Carregando pontos…' : 'Selecione um ponto…'}</option>
          {points.map((point) => (
            <option key={point.id} value={point.id}>
              {point.name} — {point.address.city}/{point.address.state}
            </option>
          ))}
        </Select>
      </Card>
      {chosenId && <DonationsList pointId={chosenId} />}
    </>
  )
}

export function DonationsPage() {
  const { user, isAdmin, isManager } = useAuth()
  const ownPointId = user?.collectionPointId ?? null

  return (
    <div className="mx-auto w-[90%] space-y-6">
      <SectionTitle
        icon={<HandHeart className="size-5" />}
        title="Doações"
        description="Acompanhe quem avisou que vai doar, confirme o recebimento e some os itens ao estoque."
      />

      <HowItWorks>
        Quando alguém avisa pelo mapa público que vai doar, a doação aparece aqui como{' '}
        <strong>Aguardando entrega</strong>. Combine a entrega pelo telefone informado. Ao receber,
        toque em <strong>Recebi</strong> e, se quiser, já some os itens ao estoque. Se a pessoa
        desistir, use <strong>Cancelar</strong>.
      </HowItWorks>

      {isAdmin && <AdminDonations />}
      {!isAdmin && isManager && ownPointId && <DonationsList pointId={ownPointId} />}
      {!isAdmin && isManager && !ownPointId && (
        <Alert tone="warning" title="Seu usuário não está vinculado a um ponto">
          Peça a um administrador para vincular você a um ponto de coleta.
        </Alert>
      )}
    </div>
  )
}
