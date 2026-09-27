import type { ColumnDef } from '@tanstack/react-table'
import { CheckCircle2, PackagePlus, PackageSearch, Pencil } from 'lucide-react'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { beneficiaryService } from '@/models/services'
import {
  cycleTypeLabels,
  itemCategoryLabels,
  type Beneficiary,
  type CollectionPoint,
  type ItemCategory,
  type Withdrawal,
} from '@/models/types'
import {
  registerWithdrawalSchema,
  useAuth,
  useBeneficiaryWithdrawalHistory,
  useCollectionPoint,
  useCollectionPoints,
  useRegisterWithdrawal,
  useUpdateWithdrawal,
} from '@/viewmodels'
import { DataTable, type RowAction } from '@/views/components/data-table'
import { ModalComponent } from '@/views/components/modal'
import { categoryIcons } from '@/views/components/stock/categoryIcons'
import { toStockQuantities } from '@/views/components/stock/stockDraft'
import { CategoryBadges } from '@/views/components/withdrawal/CategoryBadge'
import { CpfLookup } from '@/views/components/withdrawal/CpfLookup'
import { CycleUsage } from '@/views/components/withdrawal/CycleUsage'
import {
  LIMITED_CATEGORY,
  availableWithdrawalCategories,
  isLimitedCategory,
} from '@/views/components/withdrawal/withdrawalCategories'
import {
  Alert,
  Badge,
  Button,
  Card,
  FormField,
  SectionTitle,
  Select,
  TagBox,
  Textarea,
  type TagBoxOption,
} from '@/views/components/ui'

const withdrawalDateFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

function withdrawalColumns(currentPointId: string): ColumnDef<Withdrawal>[] {
  return [
    {
      accessorKey: 'occurredAtUtc',
      header: 'Data',
      meta: { label: 'Data' },
      cell: ({ getValue }) => (
        <time dateTime={getValue<string>()} className="text-sm whitespace-nowrap tabular-nums">
          {withdrawalDateFormatter.format(new Date(getValue<string>()))}
        </time>
      ),
    },
    {
      id: 'categories',
      header: 'Itens',
      accessorFn: (withdrawal) =>
        withdrawal.categories.map((category) => itemCategoryLabels[category]).join(', '),
      meta: { label: 'Itens', filterPlaceholder: 'Filtrar item…' },
      cell: ({ row }) => <CategoryBadges categories={row.original.categories} />,
    },
    {
      id: 'point',
      header: 'Ponto',
      accessorFn: (withdrawal) =>
        withdrawal.pointId === currentPointId ? 'Neste ponto' : 'Em outro ponto',
      meta: { label: 'Ponto' },
    },
    {
      accessorKey: 'notes',
      header: 'Observações',
      enableColumnFilter: false,
      meta: { label: 'Observações' },
      cell: ({ getValue }) => {
        const notes = getValue<string | null | undefined>()
        return notes ? (
          <span className="block max-w-xs truncate text-stone-600" title={notes}>
            {notes}
          </span>
        ) : (
          <span className="text-stone-400">—</span>
        )
      },
    },
  ]
}

function categoryOptionsOf(
  availableCategories: ItemCategory[],
  quantities: Record<ItemCategory, number>,
): TagBoxOption[] {
  return availableCategories.map((category) => {
    const Icon = categoryIcons[category]
    return {
      value: category,
      label: `${itemCategoryLabels[category]} (${quantities[category]})`,
      icon: <Icon className="size-4 text-stone-500" aria-hidden />,
    }
  })
}

function describeCategoriesHint(categories: ItemCategory[], hasAnyItem: boolean): string {
  if (!hasAnyItem) return 'Este ponto não tem nenhum item em estoque no momento.'
  if (categories.length === 0) return 'Escolha ao menos um item disponível.'
  return categories.some(isLimitedCategory)
    ? `${itemCategoryLabels[LIMITED_CATEGORY]} seguem o limite do ciclo; os demais itens não têm limite.`
    : 'Sem limite por ciclo — a retirada fica registrada no histórico.'
}

const MAX_NOTES_LENGTH = 500
const NOTES_ERROR_MESSAGE = 'Máximo de 500 caracteres.'

function describeRule(point: CollectionPoint): string {
  const { maxWithdrawalsPerCycle, type } = point.withdrawalRule
  const times = maxWithdrawalsPerCycle === 1 ? 'retirada' : 'retiradas'
  return `${maxWithdrawalsPerCycle} ${times} por família · ciclo ${cycleTypeLabels[type].toLowerCase()}`
}

function PointSelector({
  points,
  selectedPointId,
  onChoose,
  point,
}: {
  points: CollectionPoint[]
  selectedPointId: string
  onChoose: (pointId: string) => void
  point: CollectionPoint | null
}) {
  const hasSingleOption = points.length === 1

  return (
    <Card className="space-y-3 p-5">
      <FormField label="Ponto da retirada" htmlFor="withdrawal-point" required>
        <Select
          id="withdrawal-point"
          value={selectedPointId}
          onValueChange={onChoose}
          disabled={hasSingleOption || points.length === 0}
        >
          <option value="">Selecione o ponto…</option>
          {points.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name} — {p.address.city}/{p.address.state}
            </option>
          ))}
        </Select>
      </FormField>
      {point && (
        <p className="text-xs text-stone-500">
          Limite deste ponto: <strong className="text-stone-700">{describeRule(point)}</strong>. O
          histórico é somado entre todos os pontos do município.
        </p>
      )}
    </Card>
  )
}

interface WithdrawalFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: string
  point: CollectionPoint
  initialCategories: ItemCategory[]
  initialNotes: string
  blockedByLimit: boolean
  isPending: boolean
  errorMessage?: string
  isConflictError?: boolean
  onErrorReset: () => void
  onSubmit: (categories: ItemCategory[], notes: string | null) => Promise<void>
}

function WithdrawalFormModal({
  open,
  onOpenChange,
  title,
  description,
  point,
  initialCategories,
  initialNotes,
  blockedByLimit,
  isPending,
  errorMessage,
  isConflictError,
  onErrorReset,
  onSubmit,
}: WithdrawalFormModalProps) {
  const availableCategories = useMemo(() => availableWithdrawalCategories(point), [point])
  const quantities = useMemo(() => toStockQuantities(point), [point])
  const hasAnyItemInStock = availableCategories.length > 0

  const [categories, setCategories] = useState<ItemCategory[]>(initialCategories)
  const [notes, setNotes] = useState(initialNotes)

  useEffect(() => {
    if (!open) return
    setCategories(initialCategories)
    setNotes(initialNotes)
    onErrorReset()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const includesLimitedCategory = categories.some(isLimitedCategory)
  const isBlocked = blockedByLimit && includesLimitedCategory
  const notesTooLong = notes.length > MAX_NOTES_LENGTH
  const canSubmit = categories.length > 0 && !isBlocked && !notesTooLong

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!canSubmit) return
    const parsed = registerWithdrawalSchema.safeParse({ notes: notes.trim() })
    if (!parsed.success) return
    await onSubmit(categories, parsed.data.notes || null)
  }

  return (
    <ModalComponent open={open} onOpenChange={onOpenChange} title={title} description={description}>
      <form onSubmit={submit} className="space-y-4">
        <FormField
          label="Itens da retirada"
          htmlFor="withdrawal-categories"
          required
          hint={describeCategoriesHint(categories, hasAnyItemInStock)}
        >
          <TagBox
            id="withdrawal-categories"
            aria-label="Itens da retirada"
            options={categoryOptionsOf(availableCategories, quantities)}
            value={categories}
            placeholder="Selecione os itens retirados…"
            disabled={!hasAnyItemInStock}
            invalid={categories.length === 0}
            onValueChange={(value) => setCategories(value as ItemCategory[])}
          />
        </FormField>

        {isBlocked && (
          <Alert tone="warning">
            Este beneficiário atingiu o limite de{' '}
            {itemCategoryLabels[LIMITED_CATEGORY].toLowerCase()} do ciclo vigente neste ponto.
            Remova as {itemCategoryLabels[LIMITED_CATEGORY].toLowerCase()} da lista para registrar
            os demais itens.
          </Alert>
        )}

        <FormField
          label="Observações (opcional)"
          htmlFor="withdrawal-notes"
          error={notesTooLong ? NOTES_ERROR_MESSAGE : undefined}
          hint={`${notes.length}/${MAX_NOTES_LENGTH} · itens retirados, observações…`}
        >
          <Textarea
            id="withdrawal-notes"
            rows={2}
            invalid={notesTooLong}
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
          />
        </FormField>

        {errorMessage && (
          <Alert
            tone={isConflictError ? 'warning' : 'error'}
            title={isConflictError ? 'Não permitido' : undefined}
          >
            {errorMessage}
          </Alert>
        )}

        <Button
          type="submit"
          variant="brand"
          size="lg"
          className="w-full"
          leftIcon={<CheckCircle2 className="size-5" />}
          loading={isPending}
          disabled={!canSubmit}
        >
          {title}
        </Button>
      </form>
    </ModalComponent>
  )
}

export function RegisterWithdrawalPage() {
  const { user, isAdmin } = useAuth()
  const { points } = useCollectionPoints()
  const registerWithdrawal = useRegisterWithdrawal()
  const updateWithdrawal = useUpdateWithdrawal()

  const availablePoints = isAdmin ? points : points.filter((p) => p.id === user?.collectionPointId)
  const [chosenPointId, setChosenPointId] = useState('')
  const pointId = availablePoints.length === 1 ? availablePoints[0].id : chosenPointId
  /**
   * Consulta autenticada do ponto (não a listagem pública): é a única que traz `stockSettings`
   * (quais categorias o ponto acompanha), então é a fonte de verdade do que pode ser retirado.
   * Refeita ao abrir cada modal para nunca oferecer um item que já não está mais em estoque.
   */
  const { point, refetch: refetchPoint } = useCollectionPoint(pointId || null)

  const [beneficiary, setBeneficiary] = useState<Beneficiary | null>(null)
  const [isSearching, setIsSearching] = useState(false)
  const [lookupError, setLookupError] = useState<string | null>(null)
  const [addModalOpen, setAddModalOpen] = useState(false)
  const [editingWithdrawal, setEditingWithdrawal] = useState<Withdrawal | null>(null)
  const [feedback, setFeedback] = useState<'registered' | 'updated' | null>(null)
  const {
    history,
    isLoading: isLoadingHistory,
    error: historyError,
  } = useBeneficiaryWithdrawalHistory(beneficiary?.id ?? null, pointId || null)

  const limitReached = history?.remainingInCurrentCycle === 0
  const isInactive = beneficiary !== null && !beneficiary.isActive
  const mostRecentWithdrawalId = history?.withdrawals[0]?.id

  async function searchByCpf(cpf: string) {
    setLookupError(null)
    setFeedback(null)
    setIsSearching(true)
    try {
      setBeneficiary(await beneficiaryService.findByCpf(cpf))
    } catch {
      setLookupError('Nenhum beneficiário encontrado com esse CPF. Verifique o número.')
    } finally {
      setIsSearching(false)
    }
  }

  function changeBeneficiary() {
    setBeneficiary(null)
    setFeedback(null)
  }

  function isEditable(withdrawal: Withdrawal): boolean {
    return withdrawal.id === mostRecentWithdrawalId && withdrawal.pointId === point?.id
  }

  async function openAddModal() {
    await refetchPoint()
    setAddModalOpen(true)
  }

  async function openEditModal(withdrawal: Withdrawal) {
    await refetchPoint()
    setEditingWithdrawal(withdrawal)
  }

  const columns = useMemo(() => withdrawalColumns(point?.id ?? ''), [point?.id])
  const rowActions: RowAction<Withdrawal>[] = [
    {
      icon: <Pencil className="size-4" />,
      label: 'Editar retirada',
      onClick: (withdrawal) => void openEditModal(withdrawal),
      hidden: (withdrawal) => !isEditable(withdrawal),
    },
  ]

  return (
    <div className="mx-auto w-[90%] space-y-4">
      <SectionTitle
        icon={<PackageSearch className="size-5" />}
        title="Registrar Retirada"
        description="Busque o beneficiário pelo CPF para ver o histórico e registrar uma nova retirada."
      />

      <PointSelector
        points={availablePoints}
        selectedPointId={pointId}
        onChoose={setChosenPointId}
        point={point}
      />

      <Card className="space-y-4 p-5">
        <h2 className="text-base font-semibold text-stone-900">Beneficiário</h2>

        {!beneficiary ? (
          <CpfLookup
            disabled={!point}
            disabledHint="Selecione o ponto da retirada para buscar."
            isSearching={isSearching}
            errorMessage={lookupError}
            onSearch={searchByCpf}
          />
        ) : (
          <div className="space-y-4">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="truncate text-lg font-bold text-stone-900">{beneficiary.name}</p>
                <p className="mt-0.5 text-sm text-stone-500 tabular-nums">CPF {beneficiary.cpf}</p>
              </div>
              <Button variant="ghost" size="sm" onClick={changeBeneficiary}>
                Trocar beneficiário
              </Button>
            </div>

            {isInactive && (
              <Alert tone="warning">
                <Badge tone="red">Inativo</Badge> Este beneficiário está inativo e não pode retirar.
              </Alert>
            )}

            {feedback === 'registered' && (
              <Alert tone="success" title="Retirada registrada!">
                Já aparece na lista abaixo.
              </Alert>
            )}
            {feedback === 'updated' && (
              <Alert tone="success" title="Retirada atualizada!">
                Já aparece na lista abaixo.
              </Alert>
            )}

            {history && <CycleUsage history={history} />}
            {historyError && <Alert tone="error">{historyError.displayMessage}</Alert>}

            <div className="flex flex-wrap items-center justify-between gap-3">
              <h3 className="text-sm font-semibold text-stone-800">Retiradas deste beneficiário</h3>
              {point && (
                <Button
                  variant="brand"
                  size="sm"
                  leftIcon={<PackagePlus className="size-4" />}
                  disabled={isInactive}
                  onClick={() => void openAddModal()}
                >
                  Adicionar retirada
                </Button>
              )}
            </div>

            <DataTable
              columns={columns}
              data={history?.withdrawals ?? []}
              isLoading={isLoadingHistory}
              actions={rowActions}
              searchPlaceholder="Buscar retirada…"
              emptyMessage="Este beneficiário ainda não fez nenhuma retirada."
            />
            <p className="text-xs text-stone-500">
              Só é possível editar a retirada mais recente do beneficiário, e apenas quando ela foi
              feita neste ponto.
            </p>
          </div>
        )}
      </Card>

      {beneficiary && point && (
        <WithdrawalFormModal
          open={addModalOpen}
          onOpenChange={setAddModalOpen}
          title="Registrar retirada"
          description={`${beneficiary.name} · ${point.name}`}
          point={point}
          initialCategories={[]}
          initialNotes=""
          blockedByLimit={!!limitReached}
          isPending={registerWithdrawal.isPending}
          errorMessage={registerWithdrawal.error?.displayMessage}
          isConflictError={registerWithdrawal.error?.isConflict}
          onErrorReset={registerWithdrawal.reset}
          onSubmit={async (categories, notes) => {
            try {
              await registerWithdrawal.register({
                pointId: point.id,
                beneficiaryId: beneficiary.id,
                categories,
                notes,
              })
            } catch {
              return
            }
            setAddModalOpen(false)
            setFeedback('registered')
          }}
        />
      )}

      {editingWithdrawal && point && (
        <WithdrawalFormModal
          open
          onOpenChange={(open) => !open && setEditingWithdrawal(null)}
          title="Editar retirada"
          description={`${beneficiary?.name ?? ''} · ${point.name}`}
          point={point}
          initialCategories={editingWithdrawal.categories}
          initialNotes={editingWithdrawal.notes ?? ''}
          blockedByLimit={false}
          isPending={updateWithdrawal.isPending}
          errorMessage={updateWithdrawal.error?.displayMessage}
          isConflictError={updateWithdrawal.error?.isConflict}
          onErrorReset={updateWithdrawal.reset}
          onSubmit={async (categories, notes) => {
            try {
              await updateWithdrawal.update({
                withdrawalId: editingWithdrawal.id,
                input: { pointId: point.id, categories, notes },
              })
            } catch {
              return
            }
            setEditingWithdrawal(null)
            setFeedback('updated')
          }}
        />
      )}
    </div>
  )
}
