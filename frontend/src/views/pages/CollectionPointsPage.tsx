import type { ColumnDef } from '@tanstack/react-table'
import { Boxes, Building2, Church, Download, Landmark, MapPin, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { downloadCsv } from '@/lib/csv'
import { StockEditorModal } from '@/views/components/collection-points/StockEditorModal'
import {
  collectionPointTypeLabels,
  itemCategoryLabels,
  itemCategoryValues,
  type CollectionPoint,
  type CollectionPointType,
} from '@/models/types'
import {
  createCollectionPointSchema,
  updateCollectionPointSchema,
  useAuth,
  useCollectionPoints,
  useCreateCollectionPoint,
  useUpdateCollectionPoint,
  type CreateCollectionPointForm,
  type UpdateCollectionPointForm,
} from '@/viewmodels'
import { DataTable, type RowAction } from '@/views/components/data-table'
import { FormComponent, type FormFieldConfig, type SelectOption } from '@/views/components/form'
import { AddressLocationField } from '@/views/components/map/AddressLocationField'
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
    id: 'items',
    accessorFn: (row) => row.acceptedItems.map((i) => itemCategoryLabels[i]).join(', '),
    header: 'Itens',
    enableSorting: false,
    meta: { label: 'Itens', filterPlaceholder: 'Filtrar item…' },
    cell: ({ row }) => {
      const items = row.original.acceptedItems
      if (items.length === 0) return <span className="text-xs text-stone-400">—</span>
      return (
        <div className="flex max-w-56 flex-wrap gap-1">
          {items.map((item) => (
            <Badge key={item} tone="slate">
              {itemCategoryLabels[item]}
            </Badge>
          ))}
        </div>
      )
    },
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

const itemCategoryOptions: SelectOption[] = itemCategoryValues.map((value) => ({
  value,
  label: itemCategoryLabels[value],
}))

const collectionPointFields: FormFieldConfig[] = [
  { name: 'name', type: 'text', label: 'Nome', required: true, colSpan: 'full' },
  { name: 'type', type: 'select', label: 'Tipo', options: typeOptions, required: true },
  {
    name: 'description',
    type: 'textarea',
    label: 'Sobre o projeto',
    hint: 'Apresente a iniciativa: missão, o que faz e quem é atendido.',
    rows: 3,
    colSpan: 'full',
  },
  {
    name: 'openingHours',
    type: 'text',
    label: 'Horário de funcionamento',
    placeholder: 'Seg a Sex, 8h–17h',
    colSpan: 'full',
  },
  {
    name: 'needs',
    type: 'textarea',
    label: 'Necessidades agora',
    hint: 'Uma por linha (ex.: Fraldas P, Leite em pó). Aparecem em destaque para os doadores.',
    rows: 3,
    colSpan: 'full',
  },
  { name: 'zipCode', type: 'text', label: 'CEP', placeholder: '00000000', required: true },
  { name: 'street', type: 'text', label: 'Rua', required: true },
  { name: 'number', type: 'text', label: 'Número', required: true },
  { name: 'complement', type: 'text', label: 'Complemento' },
  { name: 'district', type: 'text', label: 'Bairro', required: true },
  { name: 'city', type: 'text', label: 'Cidade', required: true },
  { name: 'state', type: 'text', label: 'UF', maxLength: 2, placeholder: 'SP', required: true },
  {
    type: 'custom',
    key: 'addressLocation',
    colSpan: 'full',
    render: (form) => <AddressLocationField form={form} />,
  },
  { name: 'responsibleName', type: 'text', label: 'Responsável', required: true },
  { name: 'email', type: 'email', label: 'E-mail de contato', required: true },
  { name: 'phone', type: 'text', label: 'Telefone', placeholder: '11987654321', required: true },
  {
    name: 'acceptedItems',
    type: 'multiselect',
    label: 'Itens disponíveis',
    hint: 'Selecione as categorias de itens que o ponto recebe/disponibiliza.',
    options: itemCategoryOptions,
    required: true,
    colSpan: 'full',
  },
  {
    name: 'image',
    type: 'image',
    label: 'Imagem do ponto',
    hint: 'JPEG, PNG, WebP, GIF ou AVIF, até 10 MB.',
    required: true,
    colSpan: 'full',
  },
]

const collectionPointDefaults: CreateCollectionPointForm = {
  name: '',
  type: 'Association',
  description: '',
  openingHours: '',
  needs: '',
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
  acceptedItems: [],
  image: undefined as unknown as File,
}

export function CollectionPointsPage() {
  const { isAdmin, isManager } = useAuth()
  const { points, isLoading, isError, error } = useCollectionPoints()
  const { create, error: createError, createdPoint, reset } = useCreateCollectionPoint()
  const { update, error: updateError, reset: resetUpdate } = useUpdateCollectionPoint()

  const [createOpen, setCreateOpen] = useState(false)
  const [editingPoint, setEditingPoint] = useState<CollectionPoint | null>(null)
  const [stockPoint, setStockPoint] = useState<CollectionPoint | null>(null)

  function openCreate() {
    reset()
    setCreateOpen(true)
  }

  function openEdit(point: CollectionPoint) {
    resetUpdate()
    setEditingPoint(point)
  }

  const editDefaults: UpdateCollectionPointForm | null = editingPoint && {
    name: editingPoint.name,
    type: editingPoint.type,
    description: editingPoint.description ?? '',
    openingHours: editingPoint.openingHours ?? '',
    needs: editingPoint.needs.join('\n'),
    street: editingPoint.address.street,
    number: editingPoint.address.number,
    complement: editingPoint.address.complement ?? '',
    district: editingPoint.address.district,
    city: editingPoint.address.city,
    state: editingPoint.address.state,
    zipCode: editingPoint.address.zipCode,
    latitude: editingPoint.address.latitude ?? undefined,
    longitude: editingPoint.address.longitude ?? undefined,
    responsibleName: editingPoint.contact.responsibleName,
    email: editingPoint.contact.email,
    phone: editingPoint.contact.phone,
    acceptedItems: editingPoint.acceptedItems,
    image: undefined,
  }

  const editFields: FormFieldConfig[] = collectionPointFields.map((field) =>
    field.type === 'image'
      ? {
          ...field,
          required: false,
          previewUrl: editingPoint?.imageUrl,
          hint: 'Deixe como está para manter a imagem atual.',
        }
      : field,
  )

  const pointActions: RowAction<CollectionPoint>[] = [
    {
      label: 'Editar',
      icon: <Pencil className="size-4" />,
      onClick: openEdit,
      hidden: () => !isAdmin,
    },
    {
      label: 'Estoque',
      icon: <Boxes className="size-4" />,
      onClick: (point) => setStockPoint(point),
    },
    {
      label: 'Remover',
      tone: 'danger',
      icon: <Trash2 className="size-4" />,
      onClick: (point) => {
        window.alert(`Remover "${point.name}" — em breve.`)
      },
      hidden: () => !isAdmin,
    },
  ]

  return (
    <div className="mx-auto w-[90%] space-y-6">
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

      <div className="space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-base font-semibold text-stone-900">Pontos cadastrados</h2>
          <div className="flex flex-wrap gap-2">
            {points.length > 0 && (
              <Button
                variant="secondary"
                leftIcon={<Download className="size-4" />}
                onClick={() =>
                  downloadCsv(
                    'pontos-de-coleta',
                    ['Nome', 'Tipo', 'Cidade/UF', 'Avaliação', 'Nº avaliações', 'Famílias atendidas', 'Itens'],
                    points.map((p) => [
                      p.name,
                      collectionPointTypeLabels[p.type],
                      `${p.address.city}/${p.address.state}`,
                      p.averageRating.toFixed(1),
                      p.reviewCount,
                      p.familiesServed,
                      p.acceptedItems.map((i) => itemCategoryLabels[i]).join(', '),
                    ]),
                  )
                }
              >
                Exportar CSV
              </Button>
            )}
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
        </div>
        {isError && <Alert tone="error">{error?.displayMessage}</Alert>}
        <DataTable
          columns={collectionPointColumns}
          data={points}
          isLoading={isLoading}
          actions={isManager ? pointActions : undefined}
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
            columns={3}
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

      {isAdmin && editingPoint && editDefaults && (
        <ModalComponent
          open={!!editingPoint}
          onOpenChange={(open) => !open && setEditingPoint(null)}
          title="Editar ponto"
          description={`Atualize os dados de ${editingPoint.name}.`}
          size="xl"
        >
          {updateError && <Alert tone="error">{updateError.displayMessage}</Alert>}
          <FormComponent
            key={editingPoint.id}
            columns={3}
            schema={updateCollectionPointSchema}
            defaultValues={editDefaults}
            fields={editFields}
            onSubmit={async (values) => {
              await update({ id: editingPoint.id, values })
              setEditingPoint(null)
            }}
            submitLabel="Salvar alterações"
            submitIcon={<Pencil className="size-4" />}
            submitFullWidth
          />
        </ModalComponent>
      )}

      {isManager && (
        <StockEditorModal point={stockPoint} onClose={() => setStockPoint(null)} />
      )}
    </div>
  )
}
