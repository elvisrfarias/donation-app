import type { ColumnDef } from '@tanstack/react-table'
import { Building2, Church, Landmark, MapPin, Plus } from 'lucide-react'
import { useState } from 'react'
import {
  collectionPointTypeLabels,
  type CollectionPoint,
  type CollectionPointType,
} from '@/models/types'
import {
  createCollectionPointSchema,
  useAuth,
  useCollectionPoints,
  useCreateCollectionPoint,
  type CreateCollectionPointForm,
} from '@/viewmodels'
import { DataTable } from '@/views/components/data-table'
import { FormComponent, type FormFieldConfig, type SelectOption } from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Badge, Button, SectionTitle } from '@/views/components/ui'

const typeIcon: Record<CollectionPointType, typeof Building2> = {
  Association: Building2,
  Church,
  PublicEntity: Landmark,
}

const collectionPointColumns: ColumnDef<CollectionPoint>[] = [
  {
    id: 'image',
    header: 'Foto',
    enableSorting: false,
    enableColumnFilter: false,
    enableHiding: false,
    meta: { label: 'Foto' },
    cell: ({ row }) => {
      const point = row.original
      const Icon = typeIcon[point.type]
      return point.imageUrl ? (
        <img
          src={point.imageUrl}
          alt={point.name}
          loading="lazy"
          className="size-10 rounded-xl object-cover ring-1 ring-inset ring-stone-200"
        />
      ) : (
        <span className="grid size-10 place-items-center rounded-xl bg-brand-50 text-brand-600 ring-1 ring-inset ring-brand-100">
          <Icon className="size-5" />
        </span>
      )
    },
  },
  {
    accessorKey: 'name',
    header: 'Nome',
    meta: { label: 'Nome', filterPlaceholder: 'Filtrar nome…' },
    cell: ({ getValue }) => <span className="font-medium text-stone-900">{getValue<string>()}</span>,
  },
  {
    id: 'type',
    accessorFn: (row) => collectionPointTypeLabels[row.type],
    header: 'Tipo',
    meta: { label: 'Tipo', filterPlaceholder: 'Filtrar tipo…' },
    cell: ({ getValue }) => <Badge tone="brand">{getValue<string>()}</Badge>,
  },
  {
    id: 'location',
    accessorFn: (row) => `${row.address.city}/${row.address.state}`,
    header: 'Cidade/UF',
    meta: { label: 'Cidade/UF', filterPlaceholder: 'Filtrar cidade…' },
  },
  {
    accessorKey: 'contact.responsibleName',
    header: 'Responsável',
    meta: { label: 'Responsável', filterPlaceholder: 'Filtrar responsável…' },
  },
  {
    id: 'contact',
    header: 'Contato',
    enableSorting: false,
    enableColumnFilter: false,
    meta: { label: 'Contato' },
    cell: ({ row }) => (
      <div className="text-xs text-stone-500">
        <p>{row.original.contact.phone}</p>
        <p>{row.original.contact.email}</p>
      </div>
    ),
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

const typeOptions: SelectOption[] = Object.entries(collectionPointTypeLabels).map(
  ([value, label]) => ({ value, label }),
)

const collectionPointFields: FormFieldConfig[] = [
  { name: 'name', type: 'text', label: 'Nome', required: true, colSpan: 2 },
  { name: 'type', type: 'select', label: 'Tipo', options: typeOptions, required: true },
  { name: 'zipCode', type: 'text', label: 'CEP', placeholder: '00000000', required: true },
  { name: 'street', type: 'text', label: 'Rua', required: true, colSpan: 2 },
  { name: 'number', type: 'text', label: 'Número', required: true },
  { name: 'complement', type: 'text', label: 'Complemento' },
  { name: 'district', type: 'text', label: 'Bairro', required: true },
  { name: 'city', type: 'text', label: 'Cidade', required: true },
  { name: 'state', type: 'text', label: 'UF', maxLength: 2, placeholder: 'SP', required: true },
  { name: 'responsibleName', type: 'text', label: 'Responsável', required: true },
  { name: 'email', type: 'email', label: 'E-mail de contato', required: true },
  { name: 'phone', type: 'text', label: 'Telefone', placeholder: '11987654321', required: true },
  {
    name: 'image',
    type: 'image',
    label: 'Imagem do ponto',
    hint: 'JPEG, PNG, WebP, GIF ou AVIF, até 10 MB.',
    required: true,
    colSpan: 2,
  },
]

const collectionPointDefaults: CreateCollectionPointForm = {
  name: '',
  type: 'Association',
  street: '',
  number: '',
  complement: '',
  district: '',
  city: '',
  state: '',
  zipCode: '',
  responsibleName: '',
  email: '',
  phone: '',
  // Sem imagem inicial — a validação do schema exige uma ao enviar.
  image: undefined as unknown as File,
}

export function CollectionPointsPage() {
  const { isAdmin } = useAuth()
  const { points, isLoading, isError, error } = useCollectionPoints()
  const { create, error: createError, createdPoint, reset } = useCreateCollectionPoint()

  const [createOpen, setCreateOpen] = useState(false)

  function openCreate() {
    reset() // limpa erro/sucesso de tentativas anteriores
    setCreateOpen(true)
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <SectionTitle
        icon={<MapPin className="size-5" />}
        title="Pontos de Coleta"
        description="Locais cadastrados que compartilham o histórico de retiradas do município."
      />

      {createdPoint && (
        <Alert tone="success" title="Ponto criado!">
          {createdPoint.name} foi cadastrado com sucesso.
        </Alert>
      )}

      {/* Lista */}
      <div className="space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-base font-semibold text-stone-900">Pontos cadastrados</h2>
          {isAdmin ? (
            <Button variant="brand" leftIcon={<Plus className="size-4" />} onClick={openCreate}>
              Cadastrar ponto
            </Button>
          ) : (
            <span className="text-sm text-stone-500">
              Apenas administradores podem cadastrar pontos.
            </span>
          )}
        </div>
        {isError && <Alert tone="error">{error?.displayMessage}</Alert>}
        <DataTable
          columns={collectionPointColumns}
          data={points}
          isLoading={isLoading}
          searchPlaceholder="Buscar por nome, cidade, responsável…"
          emptyMessage="Nenhum ponto cadastrado ainda."
        />
      </div>

      {isAdmin && (
        <ModalComponent
          open={createOpen}
          onOpenChange={setCreateOpen}
          title="Cadastrar novo ponto"
          description="Locais que compartilham o histórico de retiradas do município."
          size="xl"
        >
          {createError && (
            <Alert tone="error">{createError.displayMessage}</Alert>
          )}
          <FormComponent
            columns={2}
            schema={createCollectionPointSchema}
            defaultValues={collectionPointDefaults}
            fields={collectionPointFields}
            onSubmit={async (values) => {
              await create(values)
              setCreateOpen(false)
            }}
            submitLabel="Cadastrar ponto"
            submitIcon={<Plus className="size-4" />}
            submitFullWidth
          />
        </ModalComponent>
      )}
    </div>
  )
}
