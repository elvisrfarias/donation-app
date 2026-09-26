import { CheckCircle2, PackageCheck, UserCog } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { beneficiaryService } from '@/models/services'
import {
  cycleTypeLabels,
  itemCategoryLabels,
  type Beneficiary,
  type CollectionPoint,
  type ItemCategory,
} from '@/models/types'
import {
  registerWithdrawalSchema,
  useAuth,
  useBeneficiaryWithdrawalHistory,
  useCollectionPoints,
  useRegisterWithdrawal,
} from '@/viewmodels'
import { categoryIcons } from '@/views/components/stock/categoryIcons'
import { toStockQuantities } from '@/views/components/stock/stockDraft'
import { CpfLookup } from '@/views/components/withdrawal/CpfLookup'
import { CycleUsage } from '@/views/components/withdrawal/CycleUsage'
import {
  LIMITED_CATEGORY,
  availableWithdrawalCategories,
  isLimitedCategory,
} from '@/views/components/withdrawal/withdrawalCategories'
import { WithdrawalHistoryList } from '@/views/components/withdrawal/WithdrawalHistoryList'
import {
  Alert,
  Badge,
  Button,
  Card,
  FormField,
  SectionTitle,
  Select,
  Spinner,
  TagBox,
  Textarea,
  type TagBoxOption,
} from '@/views/components/ui'

function defaultCategoriesFor(availableCategories: ItemCategory[]): ItemCategory[] {
  if (availableCategories.includes(LIMITED_CATEGORY)) return [LIMITED_CATEGORY]
  return availableCategories.slice(0, 1)
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

export function RegisterWithdrawalPage() {
  const { user, isAdmin } = useAuth()
  const { points } = useCollectionPoints()
  const withdrawal = useRegisterWithdrawal()

  const availablePoints = isAdmin ? points : points.filter((p) => p.id === user?.collectionPointId)
  const [chosenPointId, setChosenPointId] = useState('')
  const pointId = availablePoints.length === 1 ? availablePoints[0].id : chosenPointId
  const point = availablePoints.find((p) => p.id === pointId) ?? null

  const [beneficiary, setBeneficiary] = useState<Beneficiary | null>(null)
  const [isSearching, setIsSearching] = useState(false)
  const [lookupError, setLookupError] = useState<string | null>(null)
  const [notes, setNotes] = useState('')
  const [categories, setCategories] = useState<ItemCategory[]>([])
  const [registeredFor, setRegisteredFor] = useState<{
    name: string
    categories: ItemCategory[]
    at: string
  } | null>(null)
  const { history, isLoading: isLoadingHistory } = useBeneficiaryWithdrawalHistory(
    beneficiary?.id ?? null,
    pointId || null,
  )

  const quantities = point ? toStockQuantities(point) : ({} as Record<ItemCategory, number>)
  const availableCategories = point ? availableWithdrawalCategories(point) : []
  const hasAnyItemInStock = availableCategories.length > 0

  useEffect(() => {
    setCategories(defaultCategoriesFor(availableCategories))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [point?.id])

  const includesLimitedCategory = categories.some(isLimitedCategory)
  const limitReached = history?.remainingInCurrentCycle === 0
  const isBlockedByLimit = limitReached && includesLimitedCategory
  const isInactive = beneficiary !== null && !beneficiary.isActive
  const notesTooLong = notes.length > MAX_NOTES_LENGTH
  const canRegister =
    !!point &&
    !!history &&
    categories.length > 0 &&
    !isBlockedByLimit &&
    !isInactive &&
    !notesTooLong

  async function searchByCpf(cpf: string) {
    setLookupError(null)
    setRegisteredFor(null)
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
    withdrawal.reset()
    setBeneficiary(null)
    setNotes('')
    setCategories(defaultCategoriesFor(availableCategories))
  }

  async function register(event: FormEvent) {
    event.preventDefault()
    if (!canRegister || !beneficiary || !point) return
    const parsed = registerWithdrawalSchema.safeParse({ notes: notes.trim() })
    if (!parsed.success) return
    try {
      const registered = await withdrawal.register({
        pointId: point.id,
        beneficiaryId: beneficiary.id,
        categories,
        notes: parsed.data.notes || null,
      })
      setRegisteredFor({ name: beneficiary.name, categories, at: registered.occurredAtUtc })
      changeBeneficiary()
    } catch {
      return
    }
  }

  return (
    <div className="mx-auto w-[90%] max-w-3xl space-y-4">
      <SectionTitle
        icon={<PackageCheck className="size-5" />}
        title="Registrar Retirada"
        description="Busque o beneficiário pelo CPF, confira o saldo do ciclo e registre."
      />

      {registeredFor && (
        <Alert tone="success" title="Retirada registrada!">
          {registeredFor.categories.map((item) => itemCategoryLabels[item]).join(', ')} para{' '}
          {registeredFor.name} · {new Date(registeredFor.at).toLocaleString('pt-BR')}. Já pode
          buscar o próximo beneficiário.
        </Alert>
      )}

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
          <form onSubmit={register} className="space-y-4">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="truncate text-lg font-bold text-stone-900">{beneficiary.name}</p>
                <p className="mt-0.5 text-sm text-stone-500 tabular-nums">CPF {beneficiary.cpf}</p>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                leftIcon={<UserCog className="size-4" />}
                onClick={changeBeneficiary}
              >
                Trocar beneficiário
              </Button>
            </div>

            {isInactive && (
              <Alert tone="warning">
                <Badge tone="red">Inativo</Badge> Este beneficiário está inativo e não pode retirar.
              </Alert>
            )}

            {isLoadingHistory && <Spinner />}
            {history && <CycleUsage history={history} />}
            {history && point && (
              <WithdrawalHistoryList withdrawals={history.withdrawals} currentPointId={point.id} />
            )}

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
            {isBlockedByLimit && (
              <Alert tone="warning">
                Este beneficiário atingiu o limite de{' '}
                {itemCategoryLabels[LIMITED_CATEGORY].toLowerCase()} do ciclo vigente neste ponto.
                Remova as {itemCategoryLabels[LIMITED_CATEGORY].toLowerCase()} da lista para
                registrar os demais itens.
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

            {withdrawal.error && (
              <Alert
                tone={withdrawal.error.isConflict ? 'warning' : 'error'}
                title={withdrawal.error.isConflict ? 'Não permitido' : undefined}
              >
                {withdrawal.error.displayMessage}
              </Alert>
            )}

            <Button
              type="submit"
              variant="brand"
              size="lg"
              className="w-full"
              leftIcon={<CheckCircle2 className="size-5" />}
              loading={withdrawal.isPending}
              disabled={!canRegister}
            >
              Registrar retirada
            </Button>
          </form>
        )}
      </Card>
    </div>
  )
}
