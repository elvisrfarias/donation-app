import type { ColumnDef } from '@tanstack/react-table'
import { Pencil, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { type Beneficiary } from '@/models/types'
import {
  registerBeneficiaryWithFamilySchema,
  updateBeneficiarySchema,
  useAuth,
  useCollectionPoints,
  useFamilies,
  useRegisterBeneficiary,
  useUpdateBeneficiary,
  type RegisterBeneficiaryWithFamilyForm,
  type UpdateBeneficiaryForm,
} from '@/viewmodels'
import { DataTable, type RowAction } from '@/views/components/data-table'
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

const beneficiaryDefaults: RegisterBeneficiaryWithFamilyForm = {
  familyId: '',
  name: '',
  cpf: '',
  birthDate: '',
  registeredByPointId: '',
}

export function BeneficiariesPage() {
  const { isManager } = useAuth()
  const { points } = useCollectionPoints()
  const { families, isLoading, isError, error } = useFamilies()
  const registerBeneficiary = useRegisterBeneficiary()
  const updateBeneficiary = useUpdateBeneficiary()

  const [beneficiaryOpen, setBeneficiaryOpen] = useState(false)
  const [editingBeneficiary, setEditingBeneficiary] = useState<BeneficiaryRow | null>(null)

  const rows: BeneficiaryRow[] = families.flatMap((family) =>
    family.beneficiaries.map((beneficiary) => ({ ...beneficiary, familyName: family.name })),
  )

  function openBeneficiary() {
    registerBeneficiary.reset()
    setBeneficiaryOpen(true)
  }

  function openEditBeneficiary(beneficiary: BeneficiaryRow) {
    updateBeneficiary.reset()
    setEditingBeneficiary(beneficiary)
  }

  const beneficiaryFields: FormFieldConfig[] = [
    {
      name: 'familyId',
      type: 'autocomplete',
      label: 'Família',
      options: families.map((f) => ({ value: f.id, label: f.name })),
      placeholder: 'Selecione…',
      searchPlaceholder: 'Buscar família…',
      emptyText: 'Nenhuma família encontrada.',
      required: true,
      colSpan: 'full',
    },
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

  const editFields = beneficiaryFields.filter((field) => !('name' in field) || field.name !== 'familyId')

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
        description="Cadastre beneficiários vinculados a uma família."
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
          description="Vincule um beneficiário a uma família existente."
        >
          {registerBeneficiary.error && (
            <Alert tone="error">{registerBeneficiary.error.displayMessage}</Alert>
          )}
          <FormComponent
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
        </ModalComponent>
      )}
    </div>
  )
}
