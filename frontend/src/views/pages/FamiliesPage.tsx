import type { ColumnDef } from '@tanstack/react-table'
import { CalendarClock, Download, Pencil, Plus, Search, Trash2, UserPlus, Users } from 'lucide-react'
import { useState } from 'react'
import { z } from 'zod'
import { downloadCsv } from '@/lib/csv'
import { formatDate, formatDateTime } from '@/lib/utils'
import { cycleTypeLabels, type Beneficiary, type Withdrawal } from '@/models/types'
import {
  createFamilySchema,
  registerBeneficiarySchema,
  useAuth,
  useCollectionPoints,
  useCreateFamily,
  useFamily,
  useRegisterBeneficiary,
  type CreateFamilyForm,
  type RegisterBeneficiaryForm,
} from '@/viewmodels'
import { DataTable, type RowAction } from '@/views/components/data-table'
import { BeneficiaryKinshipsEditor } from '@/views/components/families/BeneficiaryKinshipsEditor'
import { FormComponent, type FormFieldConfig, type SelectOption } from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Badge, Button, Card, CenteredSpinner, SectionTitle } from '@/views/components/ui'

const beneficiaryColumns: ColumnDef<Beneficiary>[] = [
  {
    accessorKey: 'name',
    header: 'Nome',
    meta: { label: 'Nome', filterPlaceholder: 'Filtrar nome…' },
    cell: ({ getValue }) => <span className="font-medium text-stone-800">{getValue<string>()}</span>,
  },
  {
    accessorKey: 'cpf',
    header: 'CPF',
    meta: { label: 'CPF', filterPlaceholder: 'Filtrar CPF…' },
  },
  {
    accessorKey: 'isActive',
    header: 'Status',
    enableColumnFilter: false,
    meta: { label: 'Status', align: 'center' },
    cell: ({ getValue }) => {
      const active = getValue<boolean>()
      return <Badge tone={active ? 'green' : 'slate'}>{active ? 'Ativo' : 'Inativo'}</Badge>
    },
  },
]

const cycleOptions: SelectOption[] = Object.entries(cycleTypeLabels).map(([value, label]) => ({
  value,
  label,
}))

const familyFields: FormFieldConfig[] = [
  { name: 'name', type: 'text', label: 'Nome / referência', placeholder: 'Família Silva', required: true, colSpan: 2 },
  { name: 'cycleType', type: 'select', label: 'Ciclo', options: cycleOptions, required: true },
  { name: 'maxWithdrawalsPerCycle', type: 'number', label: 'Máx. por ciclo', min: 1, required: true },
]

const familyDefaults: CreateFamilyForm = { name: '', cycleType: 'Monthly', maxWithdrawalsPerCycle: 1 }
const beneficiaryDefaults: RegisterBeneficiaryForm = {
  name: '',
  cpf: '',
  birthDate: '',
  registeredByPointId: '',
}

const lookupSchema = z.object({ familyId: z.string() })

export function FamiliesPage() {
  const { isManager } = useAuth()
  const { points } = useCollectionPoints()
  const createFamily = useCreateFamily()

  const [familyId, setFamilyId] = useState<string | null>(null)
  const family = useFamily(familyId)
  const registerBeneficiary = useRegisterBeneficiary()

  const [newFamilyOpen, setNewFamilyOpen] = useState(false)
  const [lookupOpen, setLookupOpen] = useState(false)
  const [beneficiaryOpen, setBeneficiaryOpen] = useState(false)
  const [kinshipsBeneficiaryId, setKinshipsBeneficiaryId] = useState<string | null>(null)

  function openNewFamily() {
    createFamily.reset()
    setNewFamilyOpen(true)
  }
  function openBeneficiary() {
    registerBeneficiary.reset()
    setBeneficiaryOpen(true)
  }

  const pointName = (id: string) => points.find((p) => p.id === id)?.name ?? `${id.slice(0, 8)}…`
  const beneficiaryName = (id: string) =>
    family.family?.beneficiaries.find((b) => b.id === id)?.name ?? `${id.slice(0, 8)}…`

  const historyColumns: ColumnDef<Withdrawal>[] = [
    {
      accessorKey: 'occurredAtUtc',
      header: 'Data',
      meta: { label: 'Data' },
      cell: ({ getValue }) => (
        <span className="whitespace-nowrap text-stone-600">{formatDateTime(getValue<string>())}</span>
      ),
    },
    {
      id: 'beneficiary',
      accessorFn: (w) => beneficiaryName(w.beneficiaryId),
      header: 'Beneficiário',
      meta: { label: 'Beneficiário', filterPlaceholder: 'Filtrar beneficiário…' },
      cell: ({ getValue }) => (
        <span className="font-medium text-stone-700">{getValue<string>()}</span>
      ),
    },
    {
      id: 'point',
      accessorFn: (w) => pointName(w.pointId),
      header: 'Ponto',
      meta: { label: 'Ponto', filterPlaceholder: 'Filtrar ponto…' },
    },
    {
      accessorKey: 'notes',
      header: 'Observações',
      enableSorting: false,
      meta: { label: 'Observações' },
      cell: ({ getValue }) => {
        const notes = getValue<string | null | undefined>()
        return notes ? (
          <span className="text-stone-600">{notes}</span>
        ) : (
          <span className="text-stone-400">—</span>
        )
      },
    },
  ]

  const beneficiaryActions: RowAction<Beneficiary>[] = [
    {
      label: 'Vínculos de parentesco',
      icon: <Pencil className="size-4" />,
      onClick: (beneficiary) => setKinshipsBeneficiaryId(beneficiary.id),
    },
    {
      label: 'Remover',
      tone: 'danger',
      icon: <Trash2 className="size-4" />,
      onClick: (beneficiary) => {
        window.alert(`Remover "${beneficiary.name}" — em breve.`)
      },
    },
  ]

  const beneficiaryFields: FormFieldConfig[] = [
    { name: 'name', type: 'text', label: 'Nome', required: true },
    { name: 'cpf', type: 'text', label: 'CPF', placeholder: '000.000.000-00', required: true },
    { name: 'birthDate', type: 'date', label: 'Nascimento' },
    {
      name: 'registeredByPointId',
      type: 'autocomplete',
      label: 'Ponto de cadastro',
      options: points.map((p) => ({ value: p.id, label: p.name })),
      placeholder: 'Selecione…',
      searchPlaceholder: 'Buscar ponto…',
      emptyText: 'Nenhum ponto encontrado.',
      required: true,
    },
  ]

  return (
    <div className="mx-auto w-[90%] space-y-6">
      <SectionTitle
        icon={<Users className="size-5" />}
        title="Famílias"
        description="Cadastro, beneficiários e histórico municipal de retiradas."
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap gap-2">
          {isManager && (
            <Button variant="brand" leftIcon={<Plus className="size-4" />} onClick={openNewFamily}>
              Nova família
            </Button>
          )}
          <Button
            variant="secondary"
            leftIcon={<Search className="size-4" />}
            onClick={() => setLookupOpen(true)}
          >
            Consultar família
          </Button>
        </div>
        {!isManager && (
          <span className="text-sm text-stone-500">
            Apenas gestores/administradores podem criar famílias.
          </span>
        )}
      </div>

      {createFamily.createdFamily && (
        <Alert tone="success" title="Família criada!">
          ID: <code className="font-mono text-xs">{createFamily.createdFamily.id}</code>
        </Alert>
      )}

      {isManager && (
        <ModalComponent
          open={newFamilyOpen}
          onOpenChange={setNewFamilyOpen}
          title="Nova família"
          description="Defina o ciclo e o limite de retiradas compartilhado no município."
        >
          {createFamily.error && <Alert tone="error">{createFamily.error.displayMessage}</Alert>}
          <FormComponent
            columns={2}
            schema={createFamilySchema}
            defaultValues={familyDefaults}
            fields={familyFields}
            onSubmit={async (values) => {
              await createFamily.create(values)
              setNewFamilyOpen(false)
            }}
            submitLabel="Criar família"
            submitIcon={<Plus className="size-4" />}
            submitFullWidth
          />
        </ModalComponent>
      )}

      <ModalComponent
        open={lookupOpen}
        onOpenChange={setLookupOpen}
        title="Consultar família"
        description="Informe o ID da família para ver detalhes."
        size="md"
      >
        <FormComponent
          schema={lookupSchema}
          defaultValues={{ familyId: '' }}
          fields={[
            {
              name: 'familyId',
              type: 'text',
              label: 'ID da família (GUID)',
              placeholder: '00000000-0000-0000-0000-000000000000',
            },
          ]}
          onSubmit={(values) => {
            setFamilyId(values.familyId.trim() || null)
            setLookupOpen(false)
          }}
          submitLabel="Carregar"
          submitIcon={<Search className="size-4" />}
          submitFullWidth
        />
      </ModalComponent>

      {familyId && (
        <>
          {family.isLoading ? (
            <CenteredSpinner label="Carregando família…" />
          ) : family.notFound ? (
            <Alert tone="warning" title="Família não encontrada">
              Verifique o ID informado.
            </Alert>
          ) : family.error ? (
            <Alert tone="error">{family.error.displayMessage}</Alert>
          ) : family.family ? (
            <div className="space-y-4">
              <Card className="p-6">
                <div className="flex items-center justify-between">
                  <h3 className="text-base font-semibold text-stone-900">{family.family.name}</h3>
                  <Badge tone="brand">
                    {cycleTypeLabels[family.family.cycle.type]} ·{' '}
                    {family.family.cycle.maxWithdrawalsPerCycle}/ciclo
                  </Badge>
                </div>
                {family.history && (
                  <>
                    <div className="mt-4 grid grid-cols-3 gap-2 text-center">
                      <div className="rounded-xl bg-stone-50 p-3 ring-1 ring-inset ring-stone-100">
                        <p className="text-2xl font-bold text-stone-900 tabular-nums">
                          {family.history.usedInCurrentCycle}
                        </p>
                        <p className="mt-0.5 text-xs text-stone-500">Usadas</p>
                      </div>
                      <div className="rounded-xl bg-brand-50 p-3 ring-1 ring-inset ring-brand-100">
                        <p className="text-2xl font-bold text-brand-700 tabular-nums">
                          {family.history.remainingInCurrentCycle}
                        </p>
                        <p className="mt-0.5 text-xs text-brand-600">Restantes</p>
                      </div>
                      <div className="rounded-xl bg-stone-50 p-3 ring-1 ring-inset ring-stone-100">
                        <p className="text-2xl font-bold text-stone-900 tabular-nums">
                          {family.history.maxPerCycle}
                        </p>
                        <p className="mt-0.5 text-xs text-stone-500">Limite</p>
                      </div>
                    </div>
                    <p className="mt-2 text-xs text-stone-500">
                      Janela: {formatDate(family.history.currentCycleStartUtc)} —{' '}
                      {formatDate(family.history.currentCycleEndUtc)}
                    </p>
                  </>
                )}
              </Card>

              <div className="space-y-3">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <h3 className="flex items-center gap-2 text-base font-semibold text-stone-900">
                    <Users className="size-5 text-brand-600" /> Beneficiários
                  </h3>
                  {isManager && (
                    <Button
                      variant="secondary"
                      size="sm"
                      leftIcon={<UserPlus className="size-4" />}
                      onClick={openBeneficiary}
                    >
                      Adicionar beneficiário
                    </Button>
                  )}
                </div>
                <DataTable
                  columns={beneficiaryColumns}
                  data={family.family.beneficiaries}
                  pageSize={5}
                  pageSizeOptions={[5, 10, 20]}
                  actions={isManager ? beneficiaryActions : undefined}
                  searchPlaceholder="Buscar beneficiário…"
                  emptyMessage="Nenhum beneficiário cadastrado."
                />
                {isManager && (
                  <ModalComponent
                    open={beneficiaryOpen}
                    onOpenChange={setBeneficiaryOpen}
                    title="Adicionar beneficiário"
                    description={`Vincular um beneficiário a ${family.family.name}.`}
                  >
                    {registerBeneficiary.error && (
                      <Alert tone="error">{registerBeneficiary.error.displayMessage}</Alert>
                    )}
                    <FormComponent
                      columns={2}
                      schema={registerBeneficiarySchema}
                      defaultValues={beneficiaryDefaults}
                      fields={beneficiaryFields}
                      onSubmit={async (values) => {
                        await registerBeneficiary.register({
                          familyId: family.family!.id,
                          name: values.name,
                          cpf: values.cpf,
                          birthDate: values.birthDate || null,
                          registeredByPointId: values.registeredByPointId,
                        })
                        setBeneficiaryOpen(false)
                      }}
                      submitLabel="Adicionar"
                      submitIcon={<UserPlus className="size-4" />}
                      submitFullWidth
                    />
                  </ModalComponent>
                )}
                {isManager && (
                  <ModalComponent
                    open={!!kinshipsBeneficiaryId}
                    onOpenChange={(open) => !open && setKinshipsBeneficiaryId(null)}
                    title="Vínculos de parentesco"
                    description="Adicione o CPF de outro beneficiário e o tipo de parentesco entre eles."
                    size="md"
                  >
                    {(() => {
                      const kinshipsBeneficiary = family.family?.beneficiaries.find(
                        (b) => b.id === kinshipsBeneficiaryId,
                      )
                      return kinshipsBeneficiary ? (
                        <BeneficiaryKinshipsEditor beneficiary={kinshipsBeneficiary} />
                      ) : null
                    })()}
                  </ModalComponent>
                )}
              </div>

              <div className="space-y-3">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <h3 className="flex items-center gap-2 text-base font-semibold text-stone-900">
                    <CalendarClock className="size-5 text-brand-600" /> Histórico de retiradas
                  </h3>
                  {(family.history?.withdrawals.length ?? 0) > 0 && (
                    <Button
                      variant="secondary"
                      size="sm"
                      leftIcon={<Download className="size-4" />}
                      onClick={() => {
                        const beneficiaryName = (id: string) =>
                          family.family?.beneficiaries.find((b) => b.id === id)?.name ?? id
                        downloadCsv(
                          `retiradas-${family.family?.name ?? 'familia'}`,
                          ['Data/hora', 'Beneficiário', 'Ponto (ID)', 'Observações'],
                          (family.history?.withdrawals ?? []).map((w) => [
                            formatDateTime(w.occurredAtUtc),
                            beneficiaryName(w.beneficiaryId),
                            w.pointId,
                            w.notes ?? '',
                          ]),
                        )
                      }}
                    >
                      Exportar CSV
                    </Button>
                  )}
                </div>
                <DataTable
                  columns={historyColumns}
                  data={family.history?.withdrawals ?? []}
                  searchPlaceholder="Buscar por beneficiário ou ponto…"
                  emptyMessage="Nenhuma retirada registrada."
                />
              </div>
            </div>
          ) : null}
        </>
      )}
    </div>
  )
}
