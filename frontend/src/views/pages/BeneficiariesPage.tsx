import type { ColumnDef } from '@tanstack/react-table'
import { Pencil, Search, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { z } from 'zod'
import { type Beneficiary } from '@/models/types'
import {
  registerBeneficiarySchema,
  updateBeneficiarySchema,
  useAuth,
  useCollectionPoints,
  useFamily,
  useRegisterBeneficiary,
  useUpdateBeneficiary,
  type RegisterBeneficiaryForm,
  type UpdateBeneficiaryForm,
} from '@/viewmodels'
import { DataTable, type RowAction } from '@/views/components/data-table'
import { FormComponent, type FormFieldConfig } from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Badge, Button, CenteredSpinner, SectionTitle } from '@/views/components/ui'

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

const beneficiaryDefaults: RegisterBeneficiaryForm = {
  name: '',
  cpf: '',
  birthDate: '',
  registeredByPointId: '',
}

const lookupSchema = z.object({ familyId: z.string() })

export function BeneficiariesPage() {
  const { isManager } = useAuth()
  const { points } = useCollectionPoints()

  const [familyId, setFamilyId] = useState<string | null>(null)
  const family = useFamily(familyId)
  const registerBeneficiary = useRegisterBeneficiary(familyId ?? '')
  const updateBeneficiary = useUpdateBeneficiary(familyId ?? '')

  const [lookupOpen, setLookupOpen] = useState(false)
  const [beneficiaryOpen, setBeneficiaryOpen] = useState(false)
  const [editingBeneficiary, setEditingBeneficiary] = useState<Beneficiary | null>(null)

  function openBeneficiary() {
    registerBeneficiary.reset()
    setBeneficiaryOpen(true)
  }

  function openEditBeneficiary(beneficiary: Beneficiary) {
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
  ]

  const editDefaults: UpdateBeneficiaryForm | null = editingBeneficiary && {
    name: editingBeneficiary.name,
    cpf: editingBeneficiary.cpf,
    birthDate: editingBeneficiary.birthDate ?? '',
    registeredByPointId: editingBeneficiary.registeredByPointId,
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
        description="Cadastre beneficiários vinculados a uma família."
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <Button
          variant="secondary"
          leftIcon={<Search className="size-4" />}
          onClick={() => setLookupOpen(true)}
        >
          Consultar família
        </Button>
      </div>

      <ModalComponent
        open={lookupOpen}
        onOpenChange={setLookupOpen}
        title="Consultar família"
        description="Informe o ID da família para cadastrar beneficiários nela."
        size="md"
      >
        <FormComponent
          columns={2}
          schema={lookupSchema}
          defaultValues={{ familyId: '' }}
          fields={[
            {
              name: 'familyId',
              type: 'text',
              label: 'ID da família (GUID)',
              placeholder: '00000000-0000-0000-0000-000000000000',
              colSpan: 'full',
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
            <div className="space-y-3">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h3 className="text-base font-semibold text-stone-900">
                  Beneficiários de {family.family.name}
                </h3>
                {isManager ? (
                  <Button
                    variant="brand"
                    size="sm"
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

              <DataTable
                columns={beneficiaryColumns}
                data={family.family.beneficiaries}
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
                    fields={beneficiaryFields}
                    onSubmit={async (values) => {
                      await updateBeneficiary.update({
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
          ) : null}
        </>
      )}
    </div>
  )
}
