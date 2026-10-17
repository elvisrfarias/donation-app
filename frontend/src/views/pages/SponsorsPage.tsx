import type { ColumnDef } from '@tanstack/react-table'
import { Gem, Pencil, Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { sponsorTierLabels, sponsorTiers, type Sponsor, type SponsorTier } from '@/models/types'
import {
  createSponsorSchema,
  updateSponsorSchema,
  useCreateSponsor,
  useDeleteSponsor,
  useSponsors,
  useUpdateSponsor,
  type CreateSponsorForm,
  type UpdateSponsorForm,
} from '@/viewmodels'
import { DataTable, type RowAction } from '@/views/components/data-table'
import { FormComponent, type FormFieldConfig } from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Badge, Button, SectionTitle } from '@/views/components/ui'

const tierBadgeTone: Record<SponsorTier, 'brand' | 'amber' | 'slate'> = {
  Diamond: 'brand',
  Gold: 'amber',
  Silver: 'slate',
}

const sponsorFields: FormFieldConfig[] = [
  { name: 'name', type: 'text', label: 'Nome da instituição', required: true, maxLength: 120 },
  {
    name: 'tier',
    type: 'select',
    label: 'Categoria',
    required: true,
    options: sponsorTiers.map((tier) => ({ value: tier, label: sponsorTierLabels[tier] })),
    hint: 'Diamante para quem contribui mais; Prata para quem contribui menos.',
  },
  {
    name: 'websiteUrl',
    type: 'text',
    label: 'Site ou rede social',
    placeholder: 'https://',
    autoComplete: 'off',
  },
  {
    name: 'logo',
    type: 'image',
    label: 'Logo',
    hint: 'JPEG, PNG, WebP, GIF ou AVIF, até 10 MB. Prefira fundo transparente ou branco.',
    required: true,
    colSpan: 'full',
  },
]

const createDefaults: CreateSponsorForm = {
  name: '',
  tier: 'Silver',
  websiteUrl: '',
  logo: undefined as unknown as File,
}

const sponsorColumns: ColumnDef<Sponsor>[] = [
  {
    id: 'logo',
    header: 'Logo',
    enableColumnFilter: false,
    enableSorting: false,
    meta: { label: 'Logo' },
    cell: ({ row }) =>
      row.original.logoUrl ? (
        <img
          src={row.original.logoUrl}
          alt={`Logo de ${row.original.name}`}
          className="h-10 w-16 rounded-lg bg-white object-contain ring-1 ring-stone-200"
        />
      ) : (
        '—'
      ),
  },
  {
    accessorKey: 'name',
    header: 'Nome',
    meta: { label: 'Nome', filterPlaceholder: 'Filtrar nome…' },
    cell: ({ getValue }) => (
      <span className="font-medium text-stone-900">{getValue<string>()}</span>
    ),
  },
  {
    id: 'tier',
    accessorFn: (row) => sponsorTierLabels[row.tier],
    header: 'Categoria',
    meta: { label: 'Categoria', filterPlaceholder: 'Filtrar categoria…' },
    cell: ({ row }) => (
      <Badge tone={tierBadgeTone[row.original.tier]}>{sponsorTierLabels[row.original.tier]}</Badge>
    ),
  },
  {
    id: 'websiteUrl',
    accessorFn: (row) => row.websiteUrl ?? '—',
    header: 'Site',
    meta: { label: 'Site', filterPlaceholder: 'Filtrar site…' },
  },
  {
    accessorKey: 'isActive',
    header: 'No site',
    enableColumnFilter: false,
    meta: { label: 'No site', align: 'center' },
    cell: ({ getValue }) => {
      const active = getValue<boolean>()
      return <Badge tone={active ? 'green' : 'slate'}>{active ? 'Exibido' : 'Oculto'}</Badge>
    },
  },
]

/** Gestão dos patrocinadores mensais do projeto (só Admin). */
export function SponsorsPage() {
  const { sponsors, isLoading, isError, error } = useSponsors()
  const { create, error: createError, reset: resetCreate } = useCreateSponsor()
  const { update, error: updateError, reset: resetUpdate } = useUpdateSponsor()
  const { remove } = useDeleteSponsor()

  const [createOpen, setCreateOpen] = useState(false)
  const [editingSponsor, setEditingSponsor] = useState<Sponsor | null>(null)

  function openCreate() {
    resetCreate()
    setCreateOpen(true)
  }

  function openEdit(sponsor: Sponsor) {
    resetUpdate()
    setEditingSponsor(sponsor)
  }

  const editDefaults: UpdateSponsorForm | null = editingSponsor && {
    name: editingSponsor.name,
    tier: editingSponsor.tier,
    websiteUrl: editingSponsor.websiteUrl ?? '',
    isActive: editingSponsor.isActive,
    logo: undefined,
  }

  const editFields: FormFieldConfig[] = [
    ...sponsorFields.map((field) =>
      field.type === 'image'
        ? {
            ...field,
            required: false,
            previewUrl: editingSponsor?.logoUrl ?? undefined,
            hint: 'Deixe como está para manter a logo atual.',
          }
        : field,
    ),
    { name: 'isActive', type: 'checkbox', label: 'Exibir na página pública', colSpan: 'full' },
  ]

  const sponsorActions: RowAction<Sponsor>[] = [
    { label: 'Editar', icon: <Pencil className="size-4" />, onClick: openEdit },
    {
      label: 'Remover',
      tone: 'danger',
      icon: <Trash2 className="size-4" />,
      onClick: (sponsor) => {
        if (
          window.confirm(
            `Remover "${sponsor.name}" de vez? Para só tirar do site por um tempo, edite e desmarque "Exibir na página pública".`,
          )
        ) {
          void remove(sponsor.id)
        }
      },
    },
  ]

  return (
    <div className="mx-auto w-[90%] space-y-6">
      <SectionTitle
        icon={<Gem className="size-5" />}
        title="Patrocinadores"
        description="Instituições que patrocinam o projeto todo mês. Os ativos aparecem na página pública, do Diamante à Prata."
      />

      <div className="space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-base font-semibold text-stone-900">Patrocinadores cadastrados</h2>
          <Button variant="brand" leftIcon={<Plus className="size-4" />} onClick={openCreate}>
            Cadastrar patrocinador
          </Button>
        </div>
        {isError && <Alert tone="error">{error?.displayMessage}</Alert>}
        <DataTable
          columns={sponsorColumns}
          data={sponsors}
          isLoading={isLoading}
          actions={sponsorActions}
          searchPlaceholder="Buscar por nome ou categoria…"
          emptyMessage="Nenhum patrocinador cadastrado ainda."
        />
      </div>

      <ModalComponent
        open={createOpen}
        onOpenChange={setCreateOpen}
        title="Cadastrar patrocinador"
        description="Ele aparece na página pública assim que for cadastrado."
      >
        {createError && <Alert tone="error">{createError.displayMessage}</Alert>}
        <FormComponent
          columns={2}
          schema={createSponsorSchema}
          defaultValues={createDefaults}
          fields={sponsorFields}
          onSubmit={async (values) => {
            const created = await create(values).then(
              () => true,
              () => false,
            )
            if (created) setCreateOpen(false)
          }}
          submitLabel="Cadastrar patrocinador"
          submitIcon={<Plus className="size-4" />}
          submitFullWidth
        />
      </ModalComponent>

      {editingSponsor && editDefaults && (
        <ModalComponent
          open={!!editingSponsor}
          onOpenChange={(open) => !open && setEditingSponsor(null)}
          title="Editar patrocinador"
          description={`Atualize os dados de ${editingSponsor.name}.`}
        >
          {updateError && <Alert tone="error">{updateError.displayMessage}</Alert>}
          <FormComponent
            key={editingSponsor.id}
            columns={2}
            schema={updateSponsorSchema}
            defaultValues={editDefaults}
            fields={editFields}
            onSubmit={async (values) => {
              const updated = await update({ id: editingSponsor.id, values }).then(
                () => true,
                () => false,
              )
              if (updated) setEditingSponsor(null)
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
