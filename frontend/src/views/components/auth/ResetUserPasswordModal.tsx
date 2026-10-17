import { KeyRound } from 'lucide-react'
import type { UserAccount } from '@/models/types'
import {
  PASSWORD_POLICY_HINT,
  resetUserPasswordSchema,
  useResetUserPassword,
  type ResetUserPasswordForm,
} from '@/viewmodels'
import { FormComponent, type FormFieldConfig } from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert } from '@/views/components/ui'

interface Props {
  user: UserAccount | null
  onClose: () => void
}

const resetPasswordDefaults: ResetUserPasswordForm = { newPassword: '', confirmPassword: '' }

const resetPasswordFields: FormFieldConfig[] = [
  {
    name: 'newPassword',
    type: 'password',
    label: 'Nova senha',
    required: true,
    autoComplete: 'new-password',
    hint: PASSWORD_POLICY_HINT,
  },
  {
    name: 'confirmPassword',
    type: 'password',
    label: 'Confirme a nova senha',
    required: true,
    autoComplete: 'new-password',
  },
]

/** Redefinição administrativa da senha de outro usuário; todas as sessões dele são encerradas. */
export function ResetUserPasswordModal({ user, onClose }: Props) {
  return (
    <ModalComponent
      open={!!user}
      onOpenChange={(open) => !open && onClose()}
      title="Redefinir senha"
      description={
        user
          ? `Defina uma nova senha para ${user.fullName}. As sessões abertas dele serão encerradas.`
          : undefined
      }
      size="md"
    >
      {user && <ResetUserPasswordBody key={user.id} user={user} onDone={onClose} />}
    </ModalComponent>
  )
}

function ResetUserPasswordBody({ user, onDone }: { user: UserAccount; onDone: () => void }) {
  const { resetPassword, error } = useResetUserPassword()

  return (
    <div className="space-y-4">
      {error && <Alert tone="error">{error.displayMessage}</Alert>}
      <FormComponent
        schema={resetUserPasswordSchema}
        defaultValues={resetPasswordDefaults}
        fields={resetPasswordFields}
        onSubmit={async ({ newPassword }) => {
          await resetPassword({ id: user.id, values: { newPassword } })
          onDone()
        }}
        submitLabel="Redefinir senha"
        submitIcon={<KeyRound className="size-4" />}
        submitFullWidth
      />
    </div>
  )
}
