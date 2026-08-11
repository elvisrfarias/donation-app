import type { ColumnDef } from '@tanstack/react-table'
import { Pencil, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { type Beneficiary } from '@/models/types'
import {
  registerBeneficiaryWithFamilySchema,
  updateBeneficiarySchema,
  useAuth,
  useCollectionPoints,
  useCreateFamily,
  useFamilies,
  useRegisterBeneficiary,
  useUpdateBeneficiary,
  type RegisterBeneficiaryWithFamilyForm,
  type UpdateBeneficiaryForm,
} from '@/viewmodels'
import { DataTable, type RowAction } from '@/views/components/data-table'
import { BeneficiaryKinshipsEditor } from '@/views/components/families/BeneficiaryKinshipsEditor'
import { FormComponent, type FormFieldConfig } from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Badge, Button, SectionTitle } from '@/views/components/ui'

type BeneficiaryRow = Beneficiary & { familyName: string }

const beneficiaryColumns: ColumnDef<BeneficiaryRow>[] = [
  {
    accessorKey: 'name',
    header: 'Nome',
    meta: { label: 'Nome', filterPlaceholder: 'Filtrar nome…' },
    cell: ({ getValue }) => (
      <span className="font-medium text-stone-800">{getValue<string>()}</span>
    ),
  },
  {
    accessorKey: 'familyName',
    header: 'Família',
    meta: { label: 'Família', filterPlaceholder: 'Filtrar família…' },
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

const LAST_POINT_KEY = 'doacao.lastRegistrationPointId'

export function BeneficiariesPage() {
  const { isManager } = useAuth()
  const { points } = useCollectionPoints()
  const { families, isLoading, isError, error } = useFamilies()
  const registerBeneficiary = useRegisterBeneficiary()
  const updateBeneficiary = useUpdateBeneficiary()
  const createFamily = useCreateFamily()

  const [beneficiaryOpen, setBeneficiaryOpen] = useState(false)
  const [editingBeneficiary, setEditingBeneficiary] = useState<BeneficiaryRow | null>(null)
  const [createdFamilies, setCreatedFamilies] = useState<{ id: string; name: string }[]>([])

  const rows: BeneficiaryRow[] = families.flatMap((family) =>
    family.beneficiaries.map((beneficiary) => ({ ...beneficiary, familyName: family.name })),
  )

  const familyOptions = [
    ...families.map((f) => ({ value: f.id, label: f.name })),
    ...createdFamilies
      .filter((cf) => !families.some((f) => f.id === cf.id))
      .map((cf) => ({ value: cf.id, label: cf.name })),
  ]

  async function createFamilyInline(name: string): Promise<string | undefined> {
    try {
      const family = await createFamily.create({
        name,
        cycleType: 'Monthly',
        maxWithdrawalsPerCycle: 1,
      })
      setCreatedFamilies((prev) => [...prev, { id: family.id, name: family.name }])
      return family.id
    } catch {
      return undefined
    }
  }

  const lastPointId = typeof localStorage !== 'undefined' ? localStorage.getItem(LAST_POINT_KEY) : null
  const defaultPointId =
    points.length === 1
      ? points[0].id
      : points.some((p) => p.id === lastPointId)
        ? (lastPointId as string)
        : ''

  const beneficiaryDefaults: RegisterBeneficiaryWithFamilyForm = {
    familyId: '',
    name: '',
    cpf: '',
    birthDate: '',
    registeredByPointId: defaultPointId,
  }

  function openBeneficiary() {
    registerBeneficiary.reset()
    createFamily.reset()
    setBeneficiaryOpen(true)
  }

  function openEditBeneficiary(beneficiary: BeneficiaryRow) {
    updateBeneficiary.reset()
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
      options: points.map((p) => ({ value: p.id, label: p.name })),
      placeholder: 'Selecione…',
      searchPlaceholder: 'Buscar ponto…',
      emptyText: 'Nenhum ponto encontrado.',
      required: true,
    },
    {
      name: 'familyId',
      type: 'autocomplete',
      label: 'Família',
      options: familyOptions,
      placeholder: 'Selecione ou crie…',
      searchPlaceholder: 'Buscar ou digitar nova família…',
      emptyText: 'Digite o nome para criar uma família.',
      hint: 'Selecione a família ou digite um nome novo para criá-la na hora.',
      onCreate: createFamilyInline,
      createLabel: (name) => `Criar família “${name}”`,
      required: true,
      colSpan: 'full',
    },
  ]

  const editFields = beneficiaryFields.filter((field) => !('name' in field) || field.name !== 'familyId')

  const liveEditingBeneficiary = editingBeneficiary
    ? (rows.find((row) => row.id === editingBeneficiary.id) ?? editingBeneficiary)
    : null

  const editDefaults: UpdateBeneficiaryForm | null = editingBeneficiary && {
    name: editingBeneficiary.name,
    cpf: editingBeneficiary.cpf,
    birthDate: editingBeneficiary.birthDate ?? '',
    registeredByPointId: editingBeneficiary.registeredByPointId,
  }

  const beneficiaryActions: RowAction<BeneficiaryRow>[] = [
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
        description="Cadastre pessoas e agrupe em famílias — crie a família na hora, sem sair da tela."
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-base font-semibold text-stone-900">Beneficiários cadastrados</h2>
        {isManager ? (
          <Button variant="brand" leftIcon={<UserPlus className="size-4" />} onClick={openBeneficiary}>
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
        data={rows}
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
          description="Vincule a uma família existente ou digite um nome novo para criar a família na hora."
        >
          {registerBeneficiary.error && (
            <Alert tone="error">{registerBeneficiary.error.displayMessage}</Alert>
          )}
          {createFamily.error && (
            <Alert tone="error">{createFamily.error.displayMessage}</Alert>
          )}
          <FormComponent
            key={beneficiaryOpen ? 'add-beneficiary' : 'closed'}
            columns={2}
            schema={registerBeneficiaryWithFamilySchema}
            defaultValues={beneficiaryDefaults}
            fields={beneficiaryFields}
            onSubmit={async (values) => {
              await registerBeneficiary.register({
                familyId: values.familyId,
                name: values.name,
                cpf: values.cpf,
                birthDate: values.birthDate || null,
                registeredByPointId: values.registeredByPointId,
              })
              if (typeof localStorage !== 'undefined') {
                localStorage.setItem(LAST_POINT_KEY, values.registeredByPointId)
              }
              setBeneficiaryOpen(false)
            }}
            submitLabel="Adicionar"
            submitIcon={<UserPlus className="size-4" />}
            submitFullWidth
          />
        </ModalComponent>
      )}

      {isManager && editingBeneficiary && editDefaults && (
        <ModalComponent
          open={!!editingBeneficiary}
          onOpenChange={(open) => !open && setEditingBeneficiary(null)}
          title="Editar beneficiário"
          description={`Atualize os dados de ${editingBeneficiary.name}.`}
        >
          {updateBeneficiary.error && (
            <Alert tone="error">{updateBeneficiary.error.displayMessage}</Alert>
          )}
          <FormComponent
            key={editingBeneficiary.id}
            columns={2}
            schema={updateBeneficiarySchema}
            defaultValues={editDefaults}
            fields={editFields}
            onSubmit={async (values) => {
              await updateBeneficiary.update({
                familyId: editingBeneficiary.familyId,
                beneficiaryId: editingBeneficiary.id,
                input: {
                  name: values.name,
                  cpf: values.cpf,
                  birthDate: values.birthDate || null,
                  registeredByPointId: values.registeredByPointId,
                },
              })
              setEditingBeneficiary(null)
            }}
            submitLabel="Salvar alterações"
            submitIcon={<Pencil className="size-4" />}
            submitFullWidth
          />
          {liveEditingBeneficiary && (
            <div className="mt-4">
              <BeneficiaryKinshipsEditor beneficiary={liveEditingBeneficiary} />
            </div>
          )}
        </ModalComponent>
      )}
    </div>
  )
}
