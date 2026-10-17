import type { ColumnDef } from '@tanstack/react-table'
import { Ban, KeyRound, Pencil, UserPlus, Users as UsersIcon } from 'lucide-react'
import { useState } from 'react'
import { roleLabels, type Role, type UserAccount } from '@/models/types'
import {
  createUserSchema,
  PASSWORD_POLICY_HINT,
  updateUserSchema,
  useAuth,
  useCollectionPoint,
  useCollectionPointsForManagement,
  useCreateUser,
  useDeactivateUser,
  useUpdateUser,
  useUsers,
  type CreateUserForm,
  type UpdateUserForm,
} from '@/viewmodels'
import { ResetUserPasswordModal } from '@/views/components/auth/ResetUserPasswordModal'
import { DataTable, type RowAction } from '@/views/components/data-table'
import { FormComponent, type FormFieldConfig, type SelectOption } from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Badge, Button, SectionTitle } from '@/views/components/ui'

const assignableRolesByPointManager: Role[] = ['PointManager', 'Viewer']

export function UsersPage() {
  const { user, isAdmin } = useAuth()
  const ownPointId = user?.collectionPointId ?? ''
  const { users, isLoading, isError, error } = useUsers()
  const { points: allPoints } = useCollectionPointsForManagement({ enabled: isAdmin })
  const { point: ownPoint } = useCollectionPoint(isAdmin ? null : ownPointId)
  const { create, error: createError, reset: resetCreate } = useCreateUser()
  const { update, error: updateError, reset: resetUpdate } = useUpdateUser()
  const { deactivate } = useDeactivateUser()

  const [createOpen, setCreateOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<UserAccount | null>(null)
  const [passwordResetUser, setPasswordResetUser] = useState<UserAccount | null>(null)

  const points = isAdmin ? allPoints : ownPoint ? [ownPoint] : []
  const roleOptions: SelectOption[] = Object.entries(roleLabels)
    .filter(([value]) => isAdmin || assignableRolesByPointManager.includes(value as Role))
    .map(([value, label]) => ({ value, label }))
  const pointOptions: SelectOption[] = points.map((p) => ({ value: p.id, label: p.name }))
  const pointNameById = new Map(points.map((p) => [p.id, p.name]))

  const userFields: FormFieldConfig[] = [
    { name: 'fullName', type: 'text', label: 'Nome completo', required: true },
    { name: 'role', type: 'select', label: 'Papel', options: roleOptions, required: true },
    {
      name: 'collectionPointId',
      type: 'select',
      label: 'Ponto de coleta',
      options: pointOptions,
      placeholder: 'Nenhum (só para Admin)',
      disabled: !isAdmin,
    },
  ]

  const createFields: FormFieldConfig[] = [
    { name: 'email', type: 'email', label: 'E-mail', required: true },
    {
      name: 'password',
      type: 'password',
      label: 'Senha',
      required: true,
      hint: PASSWORD_POLICY_HINT,
    },
    ...userFields,
  ]

  const createDefaults: CreateUserForm = {
    email: '',
    password: '',
    fullName: '',
    role: 'PointManager',
    collectionPointId: isAdmin ? '' : ownPointId,
  }

  function openCreate() {
    resetCreate()
    setCreateOpen(true)
  }

  function openEdit(user: UserAccount) {
    resetUpdate()
    setEditingUser(user)
  }

  const editDefaults: UpdateUserForm | null = editingUser && {
    fullName: editingUser.fullName,
    role: editingUser.roles[0] ?? 'Viewer',
    collectionPointId: editingUser.collectionPointId ?? '',
  }

  const userColumns: ColumnDef<UserAccount>[] = [
    {
      accessorKey: 'fullName',
      header: 'Nome',
      meta: { label: 'Nome', filterPlaceholder: 'Filtrar nome…' },
      cell: ({ getValue }) => (
        <span className="font-medium text-stone-900">{getValue<string>()}</span>
      ),
    },
    {
      accessorKey: 'email',
      header: 'E-mail',
      meta: { label: 'E-mail', filterPlaceholder: 'Filtrar e-mail…' },
    },
    {
      id: 'roles',
      accessorFn: (row) => row.roles.map((r) => roleLabels[r]).join(', '),
      header: 'Papel',
      meta: { label: 'Papel', filterPlaceholder: 'Filtrar papel…' },
      cell: ({ getValue }) => <Badge tone="brand">{getValue<string>()}</Badge>,
    },
    {
      id: 'collectionPoint',
      accessorFn: (row) =>
        row.collectionPointId ? (pointNameById.get(row.collectionPointId) ?? '—') : '—',
      header: 'Ponto de coleta',
      meta: { label: 'Ponto de coleta', filterPlaceholder: 'Filtrar ponto…' },
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

  const userActions: RowAction<UserAccount>[] = [
    {
      label: 'Editar',
      icon: <Pencil className="size-4" />,
      onClick: openEdit,
    },
    {
      label: 'Redefinir senha',
      icon: <KeyRound className="size-4" />,
      hidden: (account) => account.id === user?.userId,
      onClick: setPasswordResetUser,
    },
    {
      label: 'Desativar',
      tone: 'danger',
      icon: <Ban className="size-4" />,
      hidden: (user) => !user.isActive,
      onClick: (user) => {
        if (window.confirm(`Desativar "${user.fullName}"? Ele não poderá mais fazer login.`)) {
          void deactivate(user.id)
        }
      },
    },
  ]

  return (
    <div className="mx-auto w-[90%] space-y-6">
      <SectionTitle
        icon={<UsersIcon className="size-5" />}
        title="Usuários"
        description={
          isAdmin
            ? 'Contas gerenciais: administradores, gestores de ponto e ajudantes.'
            : 'Gestores de ponto e ajudantes vinculados ao seu ponto de coleta.'
        }
      />

      <div className="space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-base font-semibold text-stone-900">Usuários cadastrados</h2>
          <Button variant="brand" leftIcon={<UserPlus className="size-4" />} onClick={openCreate}>
            Cadastrar usuário
          </Button>
        </div>
        {isError && <Alert tone="error">{error?.displayMessage}</Alert>}
        <DataTable
          columns={userColumns}
          data={users}
          isLoading={isLoading}
          actions={userActions}
          searchPlaceholder="Buscar por nome ou e-mail…"
          emptyMessage="Nenhum usuário cadastrado ainda."
        />
      </div>

      <ModalComponent
        open={createOpen}
        onOpenChange={setCreateOpen}
        title="Cadastrar usuário"
        description="Crie uma conta gerencial e defina seu papel de acesso."
      >
        {createError && <Alert tone="error">{createError.displayMessage}</Alert>}
        <FormComponent
          schema={createUserSchema}
          defaultValues={createDefaults}
          fields={createFields}
          onSubmit={async (values) => {
            await create(values)
            setCreateOpen(false)
          }}
          submitLabel="Cadastrar usuário"
          submitIcon={<UserPlus className="size-4" />}
          submitFullWidth
        />
      </ModalComponent>

      {editingUser && editDefaults && (
        <ModalComponent
          open={!!editingUser}
          onOpenChange={(open) => !open && setEditingUser(null)}
          title="Editar usuário"
          description={`Atualize o papel e o ponto de coleta de ${editingUser.fullName}.`}
        >
          {updateError && <Alert tone="error">{updateError.displayMessage}</Alert>}
          <FormComponent
            key={editingUser.id}
            schema={updateUserSchema}
            defaultValues={editDefaults}
            fields={userFields}
            onSubmit={async (values) => {
              await update({ id: editingUser.id, values })
              setEditingUser(null)
            }}
            submitLabel="Salvar alterações"
            submitIcon={<Pencil className="size-4" />}
            submitFullWidth
          />
        </ModalComponent>
      )}

      <ResetUserPasswordModal user={passwordResetUser} onClose={() => setPasswordResetUser(null)} />
    </div>
  )
}
