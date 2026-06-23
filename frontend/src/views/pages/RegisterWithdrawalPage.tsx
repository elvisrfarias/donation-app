import { CheckCircle2, PackageCheck, Search, UserCog } from 'lucide-react'
import { useState } from 'react'
import { z } from 'zod'
import {
  registerWithdrawalSchema,
  useCollectionPoints,
  useFamily,
  useRegisterWithdrawal,
  type RegisterWithdrawalForm,
} from '@/viewmodels'
import {
  FormComponent,
  type AutoCompleteOption,
  type FormFieldConfig,
  type SelectOption,
} from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Badge, Button, Card, SectionTitle } from '@/views/components/ui'

const lookupSchema = z.object({ familyId: z.string() })

export function RegisterWithdrawalPage() {
  const { points } = useCollectionPoints()
  const withdrawal = useRegisterWithdrawal()

  const [loadedFamilyId, setLoadedFamilyId] = useState<string | null>(null)
  const family = useFamily(loadedFamilyId)

  const [lookupOpen, setLookupOpen] = useState(false)
  const [withdrawalOpen, setWithdrawalOpen] = useState(false)

  const beneficiaries = family.family?.beneficiaries ?? []
  const remaining = family.history?.remainingInCurrentCycle
  const limitReached = remaining === 0

  const pointOptions: AutoCompleteOption[] = points.map((p) => ({
    value: p.id,
    label: `${p.name} — ${p.address.city}/${p.address.state}`,
  }))
  const beneficiaryOptions: SelectOption[] = beneficiaries.map((b) => ({
    value: b.id,
    label: `${b.name} (${b.cpf})`,
  }))

  const withdrawalDefaults: RegisterWithdrawalForm = {
    pointId: '',
    familyId: loadedFamilyId ?? '',
    beneficiaryId: '',
    notes: '',
  }

  const withdrawalFields: FormFieldConfig[] = [
    {
      name: 'pointId',
      type: 'autocomplete',
      label: 'Ponto de coleta',
      options: pointOptions,
      placeholder: 'Selecione o ponto…',
      searchPlaceholder: 'Buscar por nome ou cidade…',
      emptyText: 'Nenhum ponto encontrado.',
      required: true,
    },
    {
      name: 'beneficiaryId',
      type: 'select',
      label: 'Beneficiário',
      options: beneficiaryOptions,
      placeholder: 'Selecione o beneficiário…',
      required: true,
    },
    {
      name: 'notes',
      type: 'textarea',
      label: 'Observações',
      placeholder: 'Itens retirados, observações…',
    },
  ]

  function openWithdrawal() {
    withdrawal.reset() // limpa erro/sucesso anterior
    setWithdrawalOpen(true)
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <SectionTitle
        icon={<PackageCheck className="size-5" />}
        title="Registrar Retirada"
        description="A regra de equidade é validada pelo backend, somando todos os pontos do município."
      />

      {withdrawal.lastWithdrawal && (
        <Alert tone="success" title="Retirada registrada!">
          Registrada em {new Date(withdrawal.lastWithdrawal.occurredAtUtc).toLocaleString('pt-BR')}.
        </Alert>
      )}

      {/* Painel da família — saldo/limite ficam visíveis na tela */}
      <Card className="p-6">
        {!loadedFamilyId ? (
          <div className="flex flex-col items-start gap-3">
            <div>
              <h2 className="text-base font-semibold text-stone-900">Família</h2>
              <p className="mt-1 text-sm text-stone-500">
                Carregue a família para ver o saldo do ciclo e registrar a retirada.
              </p>
            </div>
            <Button
              variant="secondary"
              leftIcon={<Search className="size-4" />}
              onClick={() => setLookupOpen(true)}
            >
              Carregar família
            </Button>
          </div>
        ) : family.isFetching ? (
          <p className="text-sm text-stone-500">Carregando família…</p>
        ) : family.notFound ? (
          <div className="flex flex-col items-start gap-3">
            <Alert tone="warning">Família não encontrada para o ID informado.</Alert>
            <Button
              variant="secondary"
              leftIcon={<Search className="size-4" />}
              onClick={() => setLookupOpen(true)}
            >
              Tentar outro ID
            </Button>
          </div>
        ) : family.family ? (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 className="text-base font-semibold text-stone-900">{family.family.name}</h2>
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
                  onClick={() => setLookupOpen(true)}
                >
                  Trocar família
                </Button>
                <Button
                  variant="brand"
                  leftIcon={<CheckCircle2 className="size-4" />}
                  disabled={limitReached}
                  onClick={openWithdrawal}
                >
                  Registrar retirada
                </Button>
              </div>
            </div>
            {limitReached && (
              <Alert tone="warning">
                Esta família atingiu o limite de retiradas do ciclo vigente.
              </Alert>
            )}
          </div>
        ) : null}
      </Card>

      {/* Carregar / trocar família (busca) */}
      <ModalComponent
        open={lookupOpen}
        onOpenChange={setLookupOpen}
        title="Carregar família"
        description="Informe o GUID da família para registrar a retirada."
        size="md"
      >
        <FormComponent
          schema={lookupSchema}
          defaultValues={{ familyId: loadedFamilyId ?? '' }}
          fields={[
            {
              name: 'familyId',
              type: 'text',
              label: 'ID da família (GUID)',
              placeholder: '00000000-0000-0000-0000-000000000000',
              required: true,
            },
          ]}
          onSubmit={(values) => {
            setLoadedFamilyId(values.familyId.trim() || null)
            setLookupOpen(false)
          }}
          submitLabel="Carregar"
          submitIcon={<Search className="size-4" />}
          submitFullWidth
        />
      </ModalComponent>

      {/* Registrar retirada */}
      {family.family && (
        <ModalComponent
          open={withdrawalOpen}
          onOpenChange={setWithdrawalOpen}
          title="Registrar retirada"
          description={`Família ${family.family.name} · ${
            remaining ?? '—'
          } retirada(s) restante(s) no ciclo.`}
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
                pointId: values.pointId,
                familyId: values.familyId,
                beneficiaryId: values.beneficiaryId,
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
