import type { ColumnDef } from '@tanstack/react-table'
import { Pencil, Save, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { ApiError } from '@/models/api/problemDetails'
import { type AddBeneficiaryKinshipInput, type Beneficiary } from '@/models/types'
import {
  registerBeneficiarySchema,
  updateBeneficiarySchema,
  useAddBeneficiaryKinship,
  useAuth,
  useBeneficiaries,
  useCollectionPoints,
  useRegisterBeneficiary,
  useUpdateBeneficiary,
  type RegisterBeneficiaryForm,
  type UpdateBeneficiaryForm,
} from '@/viewmodels'
import { DataTable, type RowAction } from '@/views/components/data-table'
import { BeneficiaryKinshipsEditor } from '@/views/components/beneficiaries/BeneficiaryKinshipsEditor'
import { PendingKinshipsEditor } from '@/views/components/beneficiaries/PendingKinshipsEditor'
import { FormComponent, type FormFieldConfig } from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Badge, Button, SectionTitle } from '@/views/components/ui'

const LAST_POINT_KEY = 'doacao.lastRegistrationPointId'

const beneficiaryColumns: ColumnDef<Beneficiary>[] = [
  {
    accessorKey: 'name',
    header: 'Nome',
    meta: { label: 'Nome', filterPlaceholder: 'Filtrar nome…' },
    cell: ({ getValue }) => (
      <span className="font-medium text-stone-800">{getValue<string>()}</span>
    ),
  },
  {
    accessorKey: 'cpf',
    header: 'CPF',
    meta: { label: 'CPF', filterPlaceholder: 'Filtrar CPF…' },
  },
  {
    accessorKey: 'birthDate',
    header: 'Nascimento',
    meta: { label: 'Nascimento' },
    cell: ({ getValue }) => {
      const value = getValue<string | null | undefined>()
      return value ? (
        <span className="text-stone-600">{new Date(value).toLocaleDateString('pt-BR')}</span>
      ) : (
        <span className="text-stone-400">—</span>
      )
    },
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

export function BeneficiariesPage() {
  const { user, isAdmin, isManager } = useAuth()
  const { points } = useCollectionPoints()
  const { beneficiaries, isLoading, isError, error } = useBeneficiaries()
  const registerBeneficiary = useRegisterBeneficiary()
  const updateBeneficiary = useUpdateBeneficiary()
  const { addKinship } = useAddBeneficiaryKinship()

  const [beneficiaryOpen, setBeneficiaryOpen] = useState(false)
  const [editingBeneficiary, setEditingBeneficiary] = useState<Beneficiary | null>(null)
  const [pendingKinships, setPendingKinships] = useState<AddBeneficiaryKinshipInput[]>([])
  const [kinshipNotice, setKinshipNotice] = useState<string | null>(null)

  const availablePoints = isAdmin ? points : points.filter((p) => p.id === user?.collectionPointId)

  const lastPointId =
    typeof localStorage !== 'undefined' ? localStorage.getItem(LAST_POINT_KEY) : null
  const defaultPointId =
    availablePoints.length === 1
      ? availablePoints[0].id
      : availablePoints.some((p) => p.id === lastPointId)
        ? (lastPointId as string)
        : ''

  const beneficiaryDefaults: RegisterBeneficiaryForm = {
    name: '',
    cpf: '',
    birthDate: '',
    registeredByPointId: defaultPointId,
  }

  function openBeneficiary() {
    registerBeneficiary.reset()
    setPendingKinships([])
    setBeneficiaryOpen(true)
  }

  function openEditBeneficiary(beneficiary: Beneficiary) {
    updateBeneficiary.reset()
    setKinshipNotice(null)
    setEditingBeneficiary(beneficiary)
  }

  const beneficiaryFields: FormFieldConfig[] = [
    { name: 'name', type: 'text', label: 'Nome', required: true },
    { name: 'cpf', type: 'text', label: 'CPF', placeholder: '000.000.000-00', required: true },
    { name: 'birthDate', type: 'date', label: 'Nascimento' },
    {
      name: 'registeredByPointId',
      type: 'autocomplete',
      label: 'Ponto de cadastro',
      options: availablePoints.map((p) => ({ value: p.id, label: p.name })),
      placeholder: 'Selecione…',
      searchPlaceholder: 'Buscar ponto…',
      emptyText: 'Nenhum ponto encontrado.',
      required: true,
    },
  ]

  const liveEditingBeneficiary = editingBeneficiary
    ? (beneficiaries.find((row) => row.id === editingBeneficiary.id) ?? editingBeneficiary)
    : null

  const editDefaults: UpdateBeneficiaryForm | null = editingBeneficiary && {
    name: editingBeneficiary.name,
    cpf: editingBeneficiary.cpf,
    birthDate: editingBeneficiary.birthDate ?? '',
    registeredByPointId: editingBeneficiary.registeredByPointId,
  }

  async function linkPendingKinships(created: Beneficiary): Promise<string[]> {
    const failures: string[] = []
    for (const kinship of pendingKinships) {
      try {
        await addKinship({ beneficiaryId: created.id, input: kinship })
      } catch (error) {
        const reason = error instanceof ApiError ? error.displayMessage : 'erro inesperado.'
        failures.push(`${kinship.relatedBeneficiaryCpf} (${reason})`)
      }
    }
    return failures
  }

  function describeKinshipFailures(failures: string[]): string {
    const summary =
      failures.length === 1
        ? 'um vínculo não foi gravado'
        : `${failures.length} vínculos não foram gravados`
    return `Beneficiário salvo, mas ${summary}: ${failures.join(' ')}`
  }

  async function handleRegister(values: RegisterBeneficiaryForm) {
    const created = await registerBeneficiary.register({
      name: values.name,
      cpf: values.cpf,
      birthDate: values.birthDate || null,
      registeredByPointId: values.registeredByPointId,
    })
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(LAST_POINT_KEY, values.registeredByPointId)
    }
    const failures = await linkPendingKinships(created)
    setBeneficiaryOpen(false)
    if (failures.length > 0) {
      setKinshipNotice(describeKinshipFailures(failures))
      setEditingBeneficiary(created)
    }
  }

  async function handleUpdate(beneficiary: Beneficiary, values: UpdateBeneficiaryForm) {
    await updateBeneficiary.update({
      beneficiaryId: beneficiary.id,
      input: {
        name: values.name,
        cpf: values.cpf,
        birthDate: values.birthDate || null,
        registeredByPointId: values.registeredByPointId,
      },
    })
    setEditingBeneficiary(null)
  }

  const beneficiaryActions: RowAction<Beneficiary>[] = [
    {
      label: 'Editar',
      icon: <Pencil className="size-4" />,
      onClick: openEditBeneficiary,
    },
  ]

  return (
    <div className="mx-auto w-[90%] space-y-6">
      <SectionTitle
        icon={<UserPlus className="size-5" />}
        title="Beneficiários"
        description="Cadastre as pessoas atendidas. O limite de retiradas é definido na configuração do estoque de cada ponto."
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-base font-semibold text-stone-900">Beneficiários cadastrados</h2>
        {isManager ? (
          <Button
            variant="brand"
            leftIcon={<UserPlus className="size-4" />}
            onClick={openBeneficiary}
          >
            Adicionar beneficiário
          </Button>
        ) : (
          <span className="text-sm text-stone-500">
            Apenas gestores/administradores podem cadastrar beneficiários.
          </span>
        )}
      </div>

      {isError && <Alert tone="error">{error?.displayMessage}</Alert>}

      <DataTable
        columns={beneficiaryColumns}
        data={beneficiaries}
        isLoading={isLoading}
        actions={isManager ? beneficiaryActions : undefined}
        searchPlaceholder="Buscar beneficiário…"
        emptyMessage="Nenhum beneficiário cadastrado."
      />

      {isManager && (
        <ModalComponent
          open={beneficiaryOpen}
          onOpenChange={setBeneficiaryOpen}
          title="Adicionar beneficiário"
          description="Cadastre a pessoa e, se quiser, os vínculos de parentesco."
          size="xl"
        >
          {registerBeneficiary.error && (
            <Alert tone="error">{registerBeneficiary.error.displayMessage}</Alert>
          )}
          <FormComponent
            key={beneficiaryOpen ? 'add-beneficiary' : 'closed'}
            columns={2}
            schema={registerBeneficiarySchema}
            defaultValues={beneficiaryDefaults}
            fields={beneficiaryFields}
            onSubmit={handleRegister}
            submitLabel="Salvar"
            submitIcon={<Save className="size-4" />}
            submitFullWidth
          >
            <PendingKinshipsEditor items={pendingKinships} onChange={setPendingKinships} />
          </FormComponent>
        </ModalComponent>
      )}

      {isManager && editingBeneficiary && editDefaults && (
        <ModalComponent
          open={!!editingBeneficiary}
          onOpenChange={(open) => !open && setEditingBeneficiary(null)}
          title="Editar beneficiário"
          description={`Atualize os dados de ${editingBeneficiary.name}.`}
          size="xl"
        >
          {kinshipNotice && <Alert tone="warning">{kinshipNotice}</Alert>}
          {updateBeneficiary.error && (
            <Alert tone="error">{updateBeneficiary.error.displayMessage}</Alert>
          )}
          <FormComponent
            key={editingBeneficiary.id}
            columns={2}
            schema={updateBeneficiarySchema}
            defaultValues={editDefaults}
            fields={beneficiaryFields}
            onSubmit={(values) => handleUpdate(editingBeneficiary, values)}
            submitLabel="Salvar"
            submitIcon={<Save className="size-4" />}
            submitFullWidth
          >
            {liveEditingBeneficiary && (
              <BeneficiaryKinshipsEditor beneficiary={liveEditingBeneficiary} />
            )}
          </FormComponent>
        </ModalComponent>
      )}
    </div>
  )
}
