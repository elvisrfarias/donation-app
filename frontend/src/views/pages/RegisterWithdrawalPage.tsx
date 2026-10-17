import type { ColumnDef } from '@tanstack/react-table'
import {
  CheckCircle2,
  Minus,
  PackagePlus,
  PackageSearch,
  Pencil,
  Plus,
  ShoppingBasket,
  X,
} from 'lucide-react'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { beneficiaryService } from '@/models/services'
import {
  cycleTypeLabels,
  isLookupLinkedTo,
  type Beneficiary,
  type BeneficiaryCpfLookup,
  type BasketTemplate,
  type CollectionPoint,
  type Withdrawal,
} from '@/models/types'
import {
  registerWithdrawalSchema,
  useAuth,
  useBeneficiaryWithdrawalHistory,
  useCollectionPoint,
  useCollectionPoints,
  useLinkBeneficiaryToPoint,
  useRegisterWithdrawal,
  useUpdateWithdrawal,
} from '@/viewmodels'
import { DataTable, type RowAction } from '@/views/components/data-table'
import { ModalComponent } from '@/views/components/modal'
import {
  activeBasketTemplates,
  insufficientItemsForQuantity,
  unitsByItemId,
} from '@/views/components/stock/basketDraft'
import { clampQuantity, parseQuantity } from '@/views/components/stock/stockDraft'
import { CpfLookup } from '@/views/components/withdrawal/CpfLookup'
import { CycleUsage } from '@/views/components/withdrawal/CycleUsage'
import { LinkToPointNotice } from '@/views/components/withdrawal/LinkToPointNotice'
import { WithdrawalItemBadges } from '@/views/components/withdrawal/WithdrawalItemBadge'
import {
  availableQuantityForItem,
  availableWithdrawalItems,
  insufficientWithdrawalItems,
  type WithdrawalDraftItem,
} from '@/views/components/withdrawal/withdrawalItems'
import {
  Alert,
  Badge,
  Button,
  Card,
  FormField,
  SectionTitle,
  Select,
  Textarea,
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
      id: 'items',
      header: 'Itens',
      accessorFn: (withdrawal) =>
        withdrawal.items.map((item) => `${item.itemName} x${item.quantity}`).join(', '),
      meta: { label: 'Itens', filterPlaceholder: 'Filtrar item…' },
      cell: ({ row }) => <WithdrawalItemBadges items={row.original.items} />,
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
          <span className="text-stone-500">—</span>
        )
      },
    },
  ]
}

function fullLookupOf(beneficiary: Beneficiary): BeneficiaryCpfLookup {
  return {
    id: beneficiary.id,
    name: beneficiary.name,
    cpf: beneficiary.cpf,
    isActive: beneficiary.isActive,
    isLinkedToMyPoint: true,
    beneficiary,
  }
}

function describeItemsHint(items: WithdrawalDraftItem[], hasAnyItem: boolean): string {
  if (!hasAnyItem) return 'Este ponto não tem nenhum item em estoque no momento.'
  if (items.length === 0) return 'Escolha ao menos um item disponível.'
  return 'Sem limite por ciclo — cada item dado desconta do estoque deste ponto.'
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

export interface SelectedBasket {
  templateId: string
  quantity: number
}

function formatMissingAmount(amount: number, unit?: string): string {
  if (!unit || unit.trim().toLowerCase() === 'un') return `${amount}`
  return `${amount} ${unit}`
}

function BasketSection({
  point,
  cycleLimitReached,
  isEditing,
  basketTemplateId,
  basketQuantity,
  onChooseTemplate,
  onChangeQuantity,
}: {
  point: CollectionPoint
  cycleLimitReached: boolean
  isEditing: boolean
  basketTemplateId: string
  basketQuantity: number
  onChooseTemplate: (templateId: string) => void
  onChangeQuantity: (quantity: number) => void
}) {
  const activeTemplates = useMemo(() => activeBasketTemplates(point.basketTemplates), [point])
  const units = useMemo(() => unitsByItemId(point.stockItems), [point])
  const selectedTemplate: BasketTemplate | null =
    activeTemplates.find((template) => template.id === basketTemplateId) ?? null

  if (activeTemplates.length === 0) {
    return (
      <p className="text-xs text-stone-500">
        {point.basketTemplates.length === 0
          ? 'Nenhuma cesta montada neste ponto.'
          : 'Todas as cestas montadas deste ponto estão desativadas.'}{' '}
        <Link
          to={`/app/stock?pointId=${point.id}`}
          className="text-brand-700 font-semibold hover:underline"
        >
          Cadastrar/gerenciar na aba Cestas
        </Link>
        .
      </p>
    )
  }

  const insufficientItems = selectedTemplate
    ? insufficientItemsForQuantity(selectedTemplate.feasibility.missing, basketQuantity, units)
    : []
  const blockedByCycleLimit = selectedTemplate !== null && cycleLimitReached
  const blockedByStock = selectedTemplate !== null && insufficientItems.length > 0

  return (
    <div className="space-y-3 rounded-2xl bg-stone-50 p-4">
      <div className="flex items-center gap-2">
        <ShoppingBasket className="text-brand-600 size-4" />
        <h4 className="text-sm font-bold text-stone-900">Cesta básica (opcional)</h4>
      </div>

      {isEditing && (
        <p className="text-xs text-stone-500">
          A cesta desta retirada já vem selecionada abaixo — trocar a cesta ou a quantidade
          substitui a cesta anterior; remover a seleção cancela a cesta desta retirada.
        </p>
      )}

      <FormField label="Cesta" htmlFor="withdrawal-basket-template">
        <div className="flex items-center gap-2">
          <Select
            id="withdrawal-basket-template"
            aria-label="Cesta básica"
            value={basketTemplateId}
            placeholder="Nenhuma"
            className="flex-1"
            onValueChange={onChooseTemplate}
          >
            <option value="">Nenhuma</option>
            {activeTemplates.map((template) => (
              <option key={template.id} value={template.id}>
                {template.name} ({template.feasibility.completeBaskets} disponíveis)
              </option>
            ))}
          </Select>
          {selectedTemplate && (
            <Button type="button" variant="ghost" size="sm" onClick={() => onChooseTemplate('')}>
              Remover cesta
            </Button>
          )}
        </div>
      </FormField>

      {selectedTemplate && (
        <>
          <FormField label="Quantidade de cestas" htmlFor="withdrawal-basket-quantity">
            <div className="flex items-center gap-2">
              <button
                type="button"
                aria-label="Diminuir quantidade de cestas"
                disabled={basketQuantity <= 1}
                onClick={() => onChangeQuantity(clampQuantity(basketQuantity - 1))}
                className="grid size-10 shrink-0 place-items-center rounded-xl text-stone-600 ring-1 ring-stone-200 ring-inset hover:bg-white disabled:opacity-40"
              >
                <Minus className="size-4" />
              </button>
              <input
                id="withdrawal-basket-quantity"
                type="number"
                min={1}
                inputMode="numeric"
                value={basketQuantity}
                onFocus={(event) => event.target.select()}
                onChange={(event) =>
                  onChangeQuantity(Math.max(1, parseQuantity(event.target.value)))
                }
                className="h-10 w-16 [appearance:textfield] rounded-xl border border-stone-200 text-center text-sm font-semibold tabular-nums [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
              />
              <button
                type="button"
                aria-label="Aumentar quantidade de cestas"
                onClick={() => onChangeQuantity(clampQuantity(basketQuantity + 1))}
                className="grid size-10 shrink-0 place-items-center rounded-xl text-stone-600 ring-1 ring-stone-200 ring-inset hover:bg-white"
              >
                <Plus className="size-4" />
              </button>
            </div>
          </FormField>

          {blockedByCycleLimit && (
            <Alert tone="warning">
              Este beneficiário já atingiu o limite de cestas básicas do ciclo vigente neste ponto.
              Remova a cesta para registrar só os demais itens.
            </Alert>
          )}
          {!blockedByCycleLimit && blockedByStock && (
            <Alert tone="warning" title="Estoque insuficiente para esta cesta">
              <ul className="list-inside list-disc">
                {insufficientItems.map((item) => (
                  <li key={item.itemId}>
                    Faltam {formatMissingAmount(item.missingAmount, item.unit)} de {item.itemName}
                  </li>
                ))}
              </ul>
            </Alert>
          )}
          {!blockedByCycleLimit && !blockedByStock && (
            <Alert tone="success">Estoque disponível para esta cesta.</Alert>
          )}
        </>
      )}
    </div>
  )
}

function WithdrawalItemRow({
  item,
  max,
  disabled,
  onChangeQuantity,
  onRemove,
}: {
  item: WithdrawalDraftItem
  max: number
  disabled: boolean
  onChangeQuantity: (quantity: number) => void
  onRemove: () => void
}) {
  return (
    <li className="flex items-center gap-3 rounded-xl border border-stone-200 bg-white p-3">
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-semibold text-stone-900">{item.itemName}</p>
        <p className="text-xs text-stone-500">{item.unit}</p>
      </div>
      <div className="flex items-center gap-1.5">
        <button
          type="button"
          aria-label={`Diminuir quantidade de ${item.itemName}`}
          disabled={disabled || item.quantity <= 1}
          onClick={() => onChangeQuantity(Math.max(1, item.quantity - 1))}
          className="grid size-8 shrink-0 place-items-center rounded-lg text-stone-600 ring-1 ring-stone-200 ring-inset hover:bg-stone-50 disabled:opacity-40"
        >
          <Minus className="size-3.5" />
        </button>
        <input
          type="number"
          min={1}
          max={max}
          inputMode="numeric"
          value={item.quantity}
          disabled={disabled}
          aria-label={`Quantidade de ${item.itemName}`}
          onFocus={(event) => event.target.select()}
          onChange={(event) =>
            onChangeQuantity(Math.max(1, Math.min(max, parseQuantity(event.target.value))))
          }
          className="h-8 w-14 [appearance:textfield] rounded-lg border border-stone-200 text-center text-sm font-semibold tabular-nums [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
        />
        <button
          type="button"
          aria-label={`Aumentar quantidade de ${item.itemName}`}
          disabled={disabled || item.quantity >= max}
          onClick={() => onChangeQuantity(Math.min(max, item.quantity + 1))}
          className="grid size-8 shrink-0 place-items-center rounded-lg text-stone-600 ring-1 ring-stone-200 ring-inset hover:bg-stone-50 disabled:opacity-40"
        >
          <Plus className="size-3.5" />
        </button>
      </div>
      <button
        type="button"
        aria-label={`Remover ${item.itemName}`}
        disabled={disabled}
        onClick={onRemove}
        className="grid size-8 shrink-0 place-items-center rounded-lg text-stone-500 hover:bg-red-50 hover:text-red-600"
      >
        <X className="size-4" />
      </button>
    </li>
  )
}

interface WithdrawalFormModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: string
  point: CollectionPoint
  initialItems: WithdrawalDraftItem[]
  initialNotes: string
  initialBasketTemplateId?: string | null
  initialBasketQuantity?: number | null
  isEditing: boolean
  cycleLimitReached: boolean
  isPending: boolean
  errorMessage?: string
  isConflictError?: boolean
  onErrorReset: () => void
  onSubmit: (
    items: { stockItemId: string; quantity: number }[],
    notes: string | null,
    basket: SelectedBasket | null,
  ) => Promise<void>
}

function WithdrawalFormModal({
  open,
  onOpenChange,
  title,
  description,
  point,
  initialItems,
  initialNotes,
  initialBasketTemplateId,
  initialBasketQuantity,
  isEditing,
  cycleLimitReached,
  isPending,
  errorMessage,
  isConflictError,
  onErrorReset,
  onSubmit,
}: WithdrawalFormModalProps) {
  const availableItems = useMemo(() => availableWithdrawalItems(point), [point])
  const hasAnyItemInStock = availableItems.length > 0
  const stockItemsById = useMemo(
    () => new Map(point.stockItems.map((item) => [item.id, item])),
    [point],
  )

  const [items, setItems] = useState<WithdrawalDraftItem[]>(initialItems)
  const [itemPicker, setItemPicker] = useState('')
  const [notes, setNotes] = useState(initialNotes)
  const [basketTemplateId, setBasketTemplateId] = useState(initialBasketTemplateId ?? '')
  const [basketQuantity, setBasketQuantity] = useState(initialBasketQuantity ?? 1)

  useEffect(() => {
    if (!open) return
    // Reinicia o rascunho do formulário sempre que a modal reabre (para o mesmo componente
    // persistente, não um remount) — reset intencional, não sincronização com uma fonte externa.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setItems(initialItems)
    setItemPicker('')
    setNotes(initialNotes)
    setBasketTemplateId(initialBasketTemplateId ?? '')
    setBasketQuantity(initialBasketQuantity ?? 1)
    onErrorReset()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const reservedByStockItemId = useMemo(
    () => Object.fromEntries(initialItems.map((item) => [item.stockItemId, item.quantity])),
    [initialItems],
  )

  const pickableItems = availableItems.filter(
    (item) => !items.some((draftItem) => draftItem.stockItemId === item.id),
  )

  function addItem(stockItemId: string) {
    const stockItem = point.stockItems.find((item) => item.id === stockItemId)
    if (!stockItem) return
    setItems((previous) => [
      ...previous,
      { stockItemId: stockItem.id, itemName: stockItem.name, unit: stockItem.unit, quantity: 1 },
    ])
    setItemPicker('')
  }

  function changeItemQuantity(stockItemId: string, quantity: number) {
    setItems((previous) =>
      previous.map((item) => (item.stockItemId === stockItemId ? { ...item, quantity } : item)),
    )
  }

  function removeItem(stockItemId: string) {
    setItems((previous) => previous.filter((item) => item.stockItemId !== stockItemId))
  }

  const insufficientItems = insufficientWithdrawalItems(
    items,
    stockItemsById,
    reservedByStockItemId,
  )

  const activeTemplates = useMemo(() => activeBasketTemplates(point.basketTemplates), [point])
  const units = useMemo(() => unitsByItemId(point.stockItems), [point])
  const selectedTemplate =
    activeTemplates.find((template) => template.id === basketTemplateId) ?? null
  const basketBlocked =
    selectedTemplate !== null &&
    (cycleLimitReached ||
      insufficientItemsForQuantity(selectedTemplate.feasibility.missing, basketQuantity, units)
        .length > 0)

  const notesTooLong = notes.length > MAX_NOTES_LENGTH
  const canSubmit =
    items.length > 0 && insufficientItems.length === 0 && !basketBlocked && !notesTooLong

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!canSubmit) return
    const parsed = registerWithdrawalSchema.safeParse({ notes: notes.trim() })
    if (!parsed.success) return
    const basket = selectedTemplate
      ? { templateId: selectedTemplate.id, quantity: basketQuantity }
      : null
    await onSubmit(
      items.map((item) => ({ stockItemId: item.stockItemId, quantity: item.quantity })),
      parsed.data.notes || null,
      basket,
    )
  }

  return (
    <ModalComponent open={open} onOpenChange={onOpenChange} title={title} description={description}>
      <form onSubmit={submit} className="space-y-4">
        <FormField
          label="Itens da retirada"
          htmlFor="withdrawal-item-picker"
          required
          hint={describeItemsHint(items, hasAnyItemInStock)}
        >
          <Select
            id="withdrawal-item-picker"
            aria-label="Itens da retirada"
            value={itemPicker}
            placeholder="Adicionar item retirado…"
            disabled={!hasAnyItemInStock || pickableItems.length === 0}
            invalid={items.length === 0}
            onValueChange={addItem}
          >
            {pickableItems.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name} ({item.unit}) · {item.quantity} em estoque
              </option>
            ))}
          </Select>
        </FormField>

        {items.length === 0 ? (
          <Alert tone="info">Nenhum item adicionado ainda.</Alert>
        ) : (
          <ul className="space-y-2">
            {items.map((item) => (
              <WithdrawalItemRow
                key={item.stockItemId}
                item={item}
                max={availableQuantityForItem(
                  stockItemsById.get(item.stockItemId) ?? { quantity: 0 },
                  reservedByStockItemId[item.stockItemId] ?? 0,
                )}
                disabled={isPending}
                onChangeQuantity={(quantity) => changeItemQuantity(item.stockItemId, quantity)}
                onRemove={() => removeItem(item.stockItemId)}
              />
            ))}
          </ul>
        )}

        {insufficientItems.length > 0 && (
          <Alert tone="warning" title="Estoque insuficiente">
            <ul className="list-inside list-disc">
              {insufficientItems.map((item) => (
                <li key={item.stockItemId}>
                  Faltam {formatMissingAmount(item.missingAmount, item.unit)} de {item.itemName}
                </li>
              ))}
            </ul>
          </Alert>
        )}

        <BasketSection
          point={point}
          cycleLimitReached={cycleLimitReached}
          isEditing={isEditing}
          basketTemplateId={basketTemplateId}
          basketQuantity={basketQuantity}
          onChooseTemplate={(templateId) => {
            setBasketTemplateId(templateId)
            setBasketQuantity(1)
          }}
          onChangeQuantity={setBasketQuantity}
        />

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
  const linkBeneficiary = useLinkBeneficiaryToPoint()

  const availablePoints = isAdmin ? points : points.filter((p) => p.id === user?.collectionPointId)
  const [chosenPointId, setChosenPointId] = useState('')
  const pointId = availablePoints.length === 1 ? availablePoints[0].id : chosenPointId
  /**
   * Consulta autenticada do ponto (não a listagem pública): é a única que traz `stockItems`
   * (o catálogo granular de estoque), então é a fonte de verdade do que pode ser retirado.
   * Refeita ao abrir cada modal para nunca oferecer um item que já não está mais em estoque.
   */
  const { point, refetch: refetchPoint } = useCollectionPoint(pointId || null)

  const [beneficiary, setBeneficiary] = useState<BeneficiaryCpfLookup | null>(null)
  const isLinkedToPoint = beneficiary !== null && !!pointId && isLookupLinkedTo(beneficiary, pointId)
  const [isSearching, setIsSearching] = useState(false)
  const [lookupError, setLookupError] = useState<string | null>(null)
  const [addModalOpen, setAddModalOpen] = useState(false)
  const [editingWithdrawal, setEditingWithdrawal] = useState<Withdrawal | null>(null)
  const [feedback, setFeedback] = useState<'registered' | 'updated' | null>(null)
  const [feedbackBasketNote, setFeedbackBasketNote] = useState<string | null>(null)
  const {
    history,
    isLoading: isLoadingHistory,
    error: historyError,
  } = useBeneficiaryWithdrawalHistory(isLinkedToPoint ? beneficiary.id : null, pointId || null)

  const limitReached = history?.remainingInCurrentCycle === 0
  const isInactive = beneficiary !== null && !beneficiary.isActive
  const mostRecentWithdrawalId = history?.withdrawals[0]?.id

  async function searchByCpf(cpf: string) {
    setLookupError(null)
    setFeedback(null)
    setFeedbackBasketNote(null)
    setIsSearching(true)
    try {
      linkBeneficiary.reset()
      setBeneficiary(await beneficiaryService.lookupByCpf(cpf))
    } catch {
      setLookupError('Nenhum beneficiário encontrado com esse CPF. Verifique o número.')
    } finally {
      setIsSearching(false)
    }
  }

  async function linkToCurrentPoint() {
    if (!beneficiary || !pointId) return
    try {
      const linked = await linkBeneficiary.linkToPoint({ beneficiaryId: beneficiary.id, pointId })
      setBeneficiary(fullLookupOf(linked))
    } catch {
      return
    }
  }

  function changeBeneficiary() {
    setBeneficiary(null)
    setFeedback(null)
    setFeedbackBasketNote(null)
  }

  /**
   * Depois de registrar/editar uma retirada com cesta, busca o ponto atualizado e monta a
   * mensagem de quantas cestas completas restam — mais simples e seguro que recalcular no
   * cliente, ao custo de uma requisição extra (só quando uma cesta foi de fato usada).
   */
  async function describeRemainingBaskets(templateId: string): Promise<string | null> {
    const refreshed = await refetchPoint()
    const template = refreshed.data?.basketTemplates.find(
      (candidate) => candidate.id === templateId,
    )
    if (!template) return null
    const count = template.feasibility.completeBaskets
    return `Restam ${count} ${count === 1 ? 'cesta completa' : 'cestas completas'} de "${template.name}" neste ponto.`
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

            {!isLinkedToPoint && point && (
              <LinkToPointNotice
                pointName={point.name}
                isLinking={linkBeneficiary.isPending}
                errorMessage={linkBeneficiary.error?.displayMessage}
                onLink={() => void linkToCurrentPoint()}
              />
            )}

            {feedback === 'registered' && (
              <Alert tone="success" title="Retirada registrada!">
                Já aparece na lista abaixo.
                {feedbackBasketNote && <p className="mt-1">{feedbackBasketNote}</p>}
              </Alert>
            )}
            {feedback === 'updated' && (
              <Alert tone="success" title="Retirada atualizada!">
                Já aparece na lista abaixo.
                {feedbackBasketNote && <p className="mt-1">{feedbackBasketNote}</p>}
              </Alert>
            )}

            {isLinkedToPoint && (
              <>
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
              </>
            )}
          </div>
        )}
      </Card>

      {beneficiary && isLinkedToPoint && point && (
        <WithdrawalFormModal
          open={addModalOpen}
          onOpenChange={setAddModalOpen}
          title="Registrar retirada"
          description={`${beneficiary.name} · ${point.name}`}
          point={point}
          initialItems={[]}
          initialNotes=""
          isEditing={false}
          cycleLimitReached={!!limitReached}
          isPending={registerWithdrawal.isPending}
          errorMessage={registerWithdrawal.error?.displayMessage}
          isConflictError={registerWithdrawal.error?.isConflict}
          onErrorReset={registerWithdrawal.reset}
          onSubmit={async (items, notes, basket) => {
            try {
              await registerWithdrawal.register({
                pointId: point.id,
                beneficiaryId: beneficiary.id,
                items,
                notes,
                ...(basket && {
                  basketTemplateId: basket.templateId,
                  basketQuantity: basket.quantity,
                }),
              })
            } catch {
              return
            }
            setAddModalOpen(false)
            setFeedback('registered')
            setFeedbackBasketNote(basket ? await describeRemainingBaskets(basket.templateId) : null)
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
          initialItems={editingWithdrawal.items.map((item) => ({
            stockItemId: item.stockItemId,
            itemName: item.itemName,
            unit: item.unit,
            quantity: item.quantity,
          }))}
          initialNotes={editingWithdrawal.notes ?? ''}
          initialBasketTemplateId={editingWithdrawal.basketTemplateId}
          initialBasketQuantity={editingWithdrawal.basketQuantity}
          isEditing
          cycleLimitReached={false}
          isPending={updateWithdrawal.isPending}
          errorMessage={updateWithdrawal.error?.displayMessage}
          isConflictError={updateWithdrawal.error?.isConflict}
          onErrorReset={updateWithdrawal.reset}
          onSubmit={async (items, notes, basket) => {
            try {
              await updateWithdrawal.update({
                withdrawalId: editingWithdrawal.id,
                input: {
                  pointId: point.id,
                  items,
                  notes,
                  ...(basket && {
                    basketTemplateId: basket.templateId,
                    basketQuantity: basket.quantity,
                  }),
                },
              })
            } catch {
              return
            }
            setEditingWithdrawal(null)
            setFeedback('updated')
            setFeedbackBasketNote(basket ? await describeRemainingBaskets(basket.templateId) : null)
          }}
        />
      )}
    </div>
  )
}
