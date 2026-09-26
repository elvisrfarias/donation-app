import type { ColumnDef } from '@tanstack/react-table'
import { Pencil, Save, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { isoToBrazilian } from '@/lib/dateInput'
import { maskPhone } from '@/lib/phoneInput'
import { ApiError } from '@/models/api/problemDetails'
import { beneficiaryService } from '@/models/services'
import { type AddBeneficiaryKinshipInput, type Beneficiary } from '@/models/types'
import {
  registerBeneficiarySchema,
  updateBeneficiarySchema,
  useAddBeneficiaryKinship,
  useAuth,
  useBeneficiaries,
  useCollectionPoints,
  useLinkBeneficiaryToPoint,
  useRegisterBeneficiary,
  useUpdateBeneficiary,
  type RegisterBeneficiaryForm,
  type UpdateBeneficiaryForm,
} from '@/viewmodels'
import { DataTable, type RowAction } from '@/views/components/data-table'
import { BeneficiaryKinshipsEditor } from '@/views/components/beneficiaries/BeneficiaryKinshipsEditor'
import { ExistingBeneficiaryNotice } from '@/views/components/beneficiaries/ExistingBeneficiaryNotice'
import { WhatsAppLink } from '@/views/components/beneficiaries/WhatsAppLink'
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
    cell: ({ getValue }) => (
      <span className="whitespace-nowrap tabular-nums">{getValue<string>()}</span>
    ),
  },
  {
    accessorKey: 'email',
    header: 'E-mail',
    meta: { label: 'E-mail', filterPlaceholder: 'Filtrar e-mail…' },
    cell: ({ getValue }) => {
      const value = getValue<string | null | undefined>()
      return value ? (
        <span className="text-stone-600">{value}</span>
      ) : (
        <Badge tone="amber">Sem e-mail</Badge>
      )
    },
  },
  {
    id: 'phones',
    header: 'Contatos',
    accessorFn: (beneficiary) => beneficiary.phones.join(' · '),
    meta: { label: 'Contatos', filterPlaceholder: 'Filtrar telefone…' },
    cell: ({ row }) =>
      row.original.phones.length > 0 ? (
        <ul className="space-y-1.5">
          {row.original.phones.map((phone) => (
            <li key={phone} className="flex items-center gap-2 whitespace-nowrap">
              <span className="whitespace-nowrap text-stone-600 tabular-nums">{phone}</span>
              <WhatsAppLink phone={phone} personName={row.original.name} />
            </li>
          ))}
        </ul>
      ) : (
        <span className="text-stone-400">—</span>
      ),
  },
  {
    accessorKey: 'birthDate',
    header: 'Nascimento',
    meta: { label: 'Nascimento' },
    cell: ({ getValue }) => {
      const value = getValue<string | null | undefined>()
      return value ? (
        <span className="text-stone-600">{isoToBrazilian(value)}</span>
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

function contactPhones({ phone, secondaryPhone }: { phone?: string; secondaryPhone?: string }) {
  return [phone, secondaryPhone].filter((value): value is string => !!value)
}

interface ExistingBeneficiary {
  beneficiary: Beneficiary
  pointId: string
}

export function BeneficiariesPage() {
  const { user, isAdmin, isManager } = useAuth()
  const { points } = useCollectionPoints()
  const { beneficiaries, isLoading, isError, error } = useBeneficiaries()
  const registerBeneficiary = useRegisterBeneficiary()
  const updateBeneficiary = useUpdateBeneficiary()
  const { addKinship } = useAddBeneficiaryKinship()
  const linkBeneficiary = useLinkBeneficiaryToPoint()

  const [beneficiaryOpen, setBeneficiaryOpen] = useState(false)
  const [editingBeneficiary, setEditingBeneficiary] = useState<Beneficiary | null>(null)
  const [pendingKinships, setPendingKinships] = useState<AddBeneficiaryKinshipInput[]>([])
  const [kinshipNotice, setKinshipNotice] = useState<string | null>(null)
  const [existing, setExisting] = useState<ExistingBeneficiary | null>(null)

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
    email: '',
    birthDate: '',
    phone: '',
    secondaryPhone: '',
    registeredByPointId: defaultPointId,
  }

  function openBeneficiary() {
    registerBeneficiary.reset()
    linkBeneficiary.reset()
    setExisting(null)
    setPendingKinships([])
    setBeneficiaryOpen(true)
  }

  function openEditBeneficiary(beneficiary: Beneficiary) {
    updateBeneficiary.reset()
    setKinshipNotice(null)
    setEditingBeneficiary(beneficiary)
  }

  const personFields: FormFieldConfig[] = [
    { name: 'name', type: 'text', label: 'Nome', required: true },
    {
      name: 'cpf',
      type: 'text',
      mask: 'cpf',
      label: 'CPF',
      placeholder: '000.000.000-00',
      required: true,
    },
    {
      name: 'email',
      type: 'email',
      label: 'E-mail',
      placeholder: 'nome@exemplo.com',
      autoComplete: 'off',
      required: true,
    },
    { name: 'birthDate', type: 'date', label: 'Nascimento' },
    {
      name: 'phone',
      type: 'text',
      mask: 'phone',
      label: 'Telefone de contato',
      placeholder: '(11) 98765-4321',
      required: true,
    },
    {
      name: 'secondaryPhone',
      type: 'text',
      mask: 'phone',
      label: 'Segundo telefone',
      placeholder: '(11) 3456-7890',
    },
  ]
  const registrationPointField: FormFieldConfig = {
    name: 'registeredByPointId',
    type: 'autocomplete',
    label: 'Ponto de cadastro',
    options: availablePoints.map((p) => ({ value: p.id, label: p.name })),
    placeholder: 'Selecione…',
    searchPlaceholder: 'Buscar ponto…',
    emptyText: 'Nenhum ponto encontrado.',
    required: true,
  }
  const registerFields = [...personFields, registrationPointField]

  const liveEditingBeneficiary = editingBeneficiary
    ? (beneficiaries.find((row) => row.id === editingBeneficiary.id) ?? editingBeneficiary)
    : null

  const editDefaults: UpdateBeneficiaryForm | null = editingBeneficiary && {
    name: editingBeneficiary.name,
    cpf: editingBeneficiary.cpf,
    email: editingBeneficiary.email ?? '',
    birthDate: editingBeneficiary.birthDate ?? '',
    phone: maskPhone(editingBeneficiary.phones[0] ?? ''),
    secondaryPhone: maskPhone(editingBeneficiary.phones[1] ?? ''),
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

  function rememberPoint(pointId: string) {
    if (typeof localStorage !== 'undefined') localStorage.setItem(LAST_POINT_KEY, pointId)
  }

  async function finishRegistration(beneficiary: Beneficiary) {
    const failures = await linkPendingKinships(beneficiary)
    setBeneficiaryOpen(false)
    setKinshipNotice(failures.length > 0 ? describeKinshipFailures(failures) : null)
    if (failures.length > 0) setEditingBeneficiary(beneficiary)
  }

  async function findExistingBeneficiary(values: RegisterBeneficiaryForm) {
    const found = await beneficiaryService.findByCpf(values.cpf)
    setExisting({ beneficiary: found, pointId: values.registeredByPointId })
  }

  async function handleRegister(values: RegisterBeneficiaryForm) {
    setExisting(null)
    linkBeneficiary.reset()
    try {
      const created = await registerBeneficiary.register({
        name: values.name,
        cpf: values.cpf,
        email: values.email,
        phones: contactPhones(values),
        birthDate: values.birthDate || null,
        registeredByPointId: values.registeredByPointId,
      })
      rememberPoint(values.registeredByPointId)
      await finishRegistration(created)
    } catch (error) {
      if (!(error instanceof ApiError && error.isConflict)) throw error
      await findExistingBeneficiary(values)
    }
  }

  async function handleLinkExisting() {
    if (!existing) return
    const linked = await linkBeneficiary.linkToPoint({
      beneficiaryId: existing.beneficiary.id,
      pointId: existing.pointId,
    })
    rememberPoint(existing.pointId)
    setExisting(null)
    await finishRegistration(linked)
    setEditingBeneficiary(linked)
  }

  function handleEditExisting() {
    if (!existing) return
    setBeneficiaryOpen(false)
    openEditBeneficiary(existing.beneficiary)
    setExisting(null)
  }

  async function handleUpdate(beneficiary: Beneficiary, values: UpdateBeneficiaryForm) {
    await updateBeneficiary.update({
      beneficiaryId: beneficiary.id,
      input: {
        name: values.name,
        cpf: values.cpf,
        email: values.email,
        phones: contactPhones(values),
        birthDate: values.birthDate || null,
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
          {existing ? (
            <ExistingBeneficiaryNotice
              beneficiary={existing.beneficiary}
              pointName={
                availablePoints.find((p) => p.id === existing.pointId)?.name ?? 'seu ponto'
              }
              alreadyLinked={existing.beneficiary.linkedPointIds.includes(existing.pointId)}
              isLinking={linkBeneficiary.isPending}
              errorMessage={linkBeneficiary.error?.displayMessage}
              onLink={handleLinkExisting}
              onEdit={handleEditExisting}
            />
          ) : (
            registerBeneficiary.error && (
              <Alert tone="error">{registerBeneficiary.error.displayMessage}</Alert>
            )
          )}
          <FormComponent
            key={beneficiaryOpen ? 'add-beneficiary' : 'closed'}
            columns={2}
            schema={registerBeneficiarySchema}
            defaultValues={beneficiaryDefaults}
            fields={registerFields}
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
            fields={personFields}
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
