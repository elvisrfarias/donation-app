import { useState } from 'react'
import { KeyRound } from 'lucide-react'
import {
  changePasswordSchema,
  PASSWORD_POLICY_HINT,
  useChangePasswordViewModel,
  type ChangePasswordForm,
} from '@/viewmodels'
import { FormComponent, type FormFieldConfig } from '@/views/components/form'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Button } from '@/views/components/ui'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const changePasswordDefaults: ChangePasswordForm = {
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
}

const changePasswordFields: FormFieldConfig[] = [
  {
    name: 'currentPassword',
    type: 'password',
    label: 'Senha atual',
    required: true,
    autoComplete: 'current-password',
  },
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

/** Troca da senha do próprio usuário. Ao concluir, as sessões em outros dispositivos são encerradas. */
export function ChangePasswordModal({ open, onOpenChange }: Props) {
  return (
    <ModalComponent
      open={open}
      onOpenChange={onOpenChange}
      title="Trocar senha"
      description="Você continua conectado aqui; as sessões em outros dispositivos são encerradas."
      size="md"
    >
      {open && <ChangePasswordBody onClose={() => onOpenChange(false)} />}
    </ModalComponent>
  )
}

function ChangePasswordBody({ onClose }: { onClose: () => void }) {
  const { changePassword, error } = useChangePasswordViewModel()
  const [changed, setChanged] = useState(false)

  if (changed) {
    return (
      <div className="space-y-4">
        <Alert tone="success">Senha alterada com sucesso.</Alert>
        <Button variant="secondary" className="w-full" onClick={onClose}>
          Fechar
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {error && <Alert tone="error">{error.displayMessage}</Alert>}
      <FormComponent
        schema={changePasswordSchema}
        defaultValues={changePasswordDefaults}
        fields={changePasswordFields}
        onSubmit={async ({ currentPassword, newPassword }) => {
          await changePassword({ currentPassword, newPassword })
          setChanged(true)
        }}
        submitLabel="Trocar senha"
        submitIcon={<KeyRound className="size-4" />}
        submitFullWidth
      />
    </div>
  )
}
