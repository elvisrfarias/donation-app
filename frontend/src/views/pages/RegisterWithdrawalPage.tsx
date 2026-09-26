import { CheckCircle2, MapPin, PackageCheck, UserCog } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { beneficiaryService } from '@/models/services'
import { cycleTypeLabels, type Beneficiary, type CollectionPoint } from '@/models/types'
import {
  registerWithdrawalSchema,
  useAuth,
  useBeneficiaryWithdrawalHistory,
  useCollectionPoints,
  useRegisterWithdrawal,
} from '@/viewmodels'
import { CpfLookup } from '@/views/components/withdrawal/CpfLookup'
import { CycleUsage } from '@/views/components/withdrawal/CycleUsage'
import {
  Alert,
  Badge,
  Button,
  Card,
  FormField,
  SectionTitle,
  Select,
  Spinner,
  Textarea,
} from '@/views/components/ui'

const MAX_NOTES_LENGTH = 500
const NOTES_ERROR_MESSAGE = 'Máximo de 500 caracteres.'

function describeRule(point: CollectionPoint): string {
  const { maxWithdrawalsPerCycle, type } = point.withdrawalRule
  const times = maxWithdrawalsPerCycle === 1 ? 'retirada' : 'retiradas'
  return `${maxWithdrawalsPerCycle} ${times} por família · ciclo ${cycleTypeLabels[type].toLowerCase()}`
}

function PointSelector({
  points,
  isAdmin,
  chosenPointId,
  onChoose,
  point,
}: {
  points: CollectionPoint[]
  isAdmin: boolean
  chosenPointId: string
  onChoose: (pointId: string) => void
  point: CollectionPoint | null
}) {
  return (
    <Card className="space-y-3 p-5">
      {isAdmin ? (
        <FormField label="Ponto da retirada" htmlFor="withdrawal-point" required>
          <Select
            id="withdrawal-point"
            value={chosenPointId}
            onValueChange={onChoose}
            disabled={points.length === 0}
          >
            <option value="">Selecione o ponto…</option>
            {points.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name} — {p.address.city}/{p.address.state}
              </option>
            ))}
          </Select>
        </FormField>
      ) : (
        point && (
          <p className="flex items-center gap-2 text-sm text-stone-600">
            <MapPin className="size-4 shrink-0 text-stone-400" />
            <span>
              Ponto da retirada: <strong className="text-stone-800">{point.name}</strong>
            </span>
          </p>
        )
      )}
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
  const [registeredFor, setRegisteredFor] = useState<{ name: string; at: string } | null>(null)
  const { history, isLoading: isLoadingHistory } = useBeneficiaryWithdrawalHistory(
    beneficiary?.id ?? null,
    pointId || null,
  )

  const limitReached = history?.remainingInCurrentCycle === 0
  const isInactive = beneficiary !== null && !beneficiary.isActive
  const notesTooLong = notes.length > MAX_NOTES_LENGTH
  const canRegister = !!point && !!history && !limitReached && !isInactive && !notesTooLong

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
        notes: parsed.data.notes || null,
      })
      setRegisteredFor({ name: beneficiary.name, at: registered.occurredAtUtc })
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
          {registeredFor.name} · {new Date(registeredFor.at).toLocaleString('pt-BR')}. Já pode
          buscar o próximo beneficiário.
        </Alert>
      )}

      <PointSelector
        points={availablePoints}
        isAdmin={isAdmin}
        chosenPointId={chosenPointId}
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
            {history && point && <CycleUsage history={history} currentPointId={point.id} />}
            {limitReached && (
              <Alert tone="warning">
                Este beneficiário atingiu o limite de retiradas do ciclo vigente neste ponto.
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
