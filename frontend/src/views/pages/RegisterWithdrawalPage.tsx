import { CheckCircle2, MapPin, PackageCheck, Search, UserCog } from 'lucide-react'
import { useState } from 'react'
import { z } from 'zod'
import { beneficiaryService } from '@/models/services'
import { cycleTypeLabels, type Beneficiary } from '@/models/types'
import {
  registerWithdrawalSchema,
  useAuth,
  useBeneficiaryWithdrawalHistory,
  useCollectionPoints,
  useRegisterWithdrawal,
  type RegisterWithdrawalForm,
} from '@/viewmodels'
import { FormComponent, type FormFieldConfig } from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Badge, Button, Card, FormField, SectionTitle, Select } from '@/views/components/ui'

const lookupSchema = z.object({ cpf: z.string().min(11, 'Informe o CPF (11 dígitos).') })

const withdrawalFields: FormFieldConfig[] = [
  {
    name: 'notes',
    type: 'textarea',
    label: 'Observações',
    placeholder: 'Itens retirados, observações…',
  },
]

export function RegisterWithdrawalPage() {
  const { user, isAdmin } = useAuth()
  const { points } = useCollectionPoints()
  const withdrawal = useRegisterWithdrawal()

  const availablePoints = isAdmin ? points : points.filter((p) => p.id === user?.collectionPointId)
  const [chosenPointId, setChosenPointId] = useState('')
  const pointId = availablePoints.length === 1 ? availablePoints[0].id : chosenPointId
  const point = availablePoints.find((p) => p.id === pointId) ?? null

  const [beneficiary, setBeneficiary] = useState<Beneficiary | null>(null)
  const { history } = useBeneficiaryWithdrawalHistory(beneficiary?.id ?? null, pointId || null)

  const [lookupOpen, setLookupOpen] = useState(false)
  const [lookupError, setLookupError] = useState<string | null>(null)
  const [withdrawalOpen, setWithdrawalOpen] = useState(false)

  async function loadByCpf(cpf: string) {
    setLookupError(null)
    try {
      setBeneficiary(await beneficiaryService.findByCpf(cpf))
      setLookupOpen(false)
    } catch {
      setLookupError('Nenhum beneficiário encontrado com esse CPF. Verifique o número.')
    }
  }

  function openLookup() {
    setLookupError(null)
    setLookupOpen(true)
  }

  function openWithdrawal() {
    withdrawal.reset()
    setWithdrawalOpen(true)
  }

  const remaining = history?.remainingInCurrentCycle
  const limitReached = remaining === 0
  const canRegister = !!point && !limitReached
  const ruleText = point
    ? `${point.withdrawalRule.maxWithdrawalsPerCycle} por família (${cycleTypeLabels[point.withdrawalRule.type].toLowerCase()})`
    : null

  const withdrawalDefaults: RegisterWithdrawalForm = { notes: '' }

  return (
    <div className="mx-auto w-[90%] space-y-6">
      <SectionTitle
        icon={<PackageCheck className="size-5" />}
        title="Registrar Retirada"
        description="O histórico é somado entre todos os pontos do município; o limite é o do ponto da retirada."
      />

      {withdrawal.lastWithdrawal && (
        <Alert tone="success" title="Retirada registrada!">
          Registrada em {new Date(withdrawal.lastWithdrawal.occurredAtUtc).toLocaleString('pt-BR')}.
        </Alert>
      )}

      <Card className="space-y-5 p-6">
        {isAdmin ? (
          <FormField label="Ponto da retirada" htmlFor="withdrawal-point">
            <Select
              id="withdrawal-point"
              value={chosenPointId}
              onValueChange={setChosenPointId}
              disabled={availablePoints.length === 0}
            >
              <option value="">Selecione o ponto…</option>
              {availablePoints.map((p) => (
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
        {ruleText && <p className="text-sm text-stone-500">Limite deste ponto: {ruleText}.</p>}

        {!beneficiary ? (
          <div className="flex flex-col items-start gap-3">
            <div>
              <h2 className="text-base font-semibold text-stone-900">Beneficiário</h2>
              <p className="mt-1 text-sm text-stone-500">
                Busque o beneficiário pelo CPF para conferir o limite do ciclo e registrar a
                retirada.
              </p>
            </div>
            <Button
              variant="secondary"
              leftIcon={<Search className="size-4" />}
              onClick={openLookup}
            >
              Buscar por CPF
            </Button>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 className="text-base font-semibold text-stone-900">{beneficiary.name}</h2>
                <p className="mt-0.5 text-sm text-stone-500">CPF {beneficiary.cpf}</p>
                {remaining !== undefined && (
                  <Badge tone={limitReached ? 'red' : 'green'} className="mt-1.5">
                    {limitReached
                      ? 'Limite do ciclo atingido'
                      : `${remaining} retirada(s) restante(s)`}
                  </Badge>
                )}
              </div>
              <div className="flex flex-wrap gap-2">
                <Button
                  variant="ghost"
                  leftIcon={<UserCog className="size-4" />}
                  onClick={openLookup}
                >
                  Trocar beneficiário
                </Button>
                <Button
                  variant="brand"
                  leftIcon={<CheckCircle2 className="size-4" />}
                  disabled={!canRegister}
                  onClick={openWithdrawal}
                >
                  Registrar retirada
                </Button>
              </div>
            </div>
            {!point && (
              <Alert tone="info">Selecione o ponto da retirada para ver o saldo do ciclo.</Alert>
            )}
            {limitReached && (
              <Alert tone="warning">
                Este beneficiário atingiu o limite de retiradas do ciclo vigente neste ponto.
              </Alert>
            )}
          </div>
        )}
      </Card>

      <ModalComponent
        open={lookupOpen}
        onOpenChange={setLookupOpen}
        title="Buscar beneficiário por CPF"
        description="Informe o CPF para carregar o beneficiário e o limite do ciclo."
        size="md"
      >
        {lookupError && (
          <div className="mb-3">
            <Alert tone="warning">{lookupError}</Alert>
          </div>
        )}
        <FormComponent
          schema={lookupSchema}
          defaultValues={{ cpf: '' }}
          fields={[
            {
              name: 'cpf',
              type: 'text',
              label: 'CPF do beneficiário',
              placeholder: '000.000.000-00',
              required: true,
            },
          ]}
          onSubmit={(values) => loadByCpf(values.cpf)}
          submitLabel="Buscar beneficiário"
          submitIcon={<Search className="size-4" />}
          submitFullWidth
        />
      </ModalComponent>

      {beneficiary && point && (
        <ModalComponent
          open={withdrawalOpen}
          onOpenChange={setWithdrawalOpen}
          title="Registrar retirada"
          description={`${beneficiary.name} · ${point.name} · ${remaining ?? '—'} retirada(s) restante(s) no ciclo.`}
        >
          {withdrawal.error && (
            <Alert
              tone={withdrawal.error.isConflict ? 'warning' : 'error'}
              title={withdrawal.error.isConflict ? 'Não permitido' : undefined}
            >
              {withdrawal.error.displayMessage}
            </Alert>
          )}
          <FormComponent
            schema={registerWithdrawalSchema}
            defaultValues={withdrawalDefaults}
            fields={withdrawalFields}
            onSubmit={async (values) => {
              await withdrawal.register({
                pointId: point.id,
                beneficiaryId: beneficiary.id,
                notes: values.notes || null,
              })
              setWithdrawalOpen(false)
            }}
            submitLabel="Registrar retirada"
            submitIcon={<CheckCircle2 className="size-5" />}
            submitDisabled={limitReached}
            submitFullWidth
          />
        </ModalComponent>
      )}
    </div>
  )
}
