import { useEffect, useState } from 'react'
import { KeyRound, LogIn } from 'lucide-react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import {
  PASSWORD_POLICY_HINT,
  resetUserPasswordSchema,
  useCompletePasswordReset,
  type ResetUserPasswordForm,
} from '@/viewmodels'
import { AuthPageLayout } from '@/views/components/auth/AuthPageLayout'
import { FormComponent, type FormFieldConfig } from '@/views/components/form'
import { Alert, Button } from '@/views/components/ui'

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

const backToLogin = { to: '/login', label: 'Voltar para o login' }

/** O token chega no fragmento (`#token=...`), que o navegador nunca envia ao servidor. */
function tokenFromHash(hash: string): string | null {
  return new URLSearchParams(hash.replace(/^#/, '')).get('token')
}

function RequestNewLink() {
  return (
    <Link to="/forgot-password" className="text-brand-600 hover:text-brand-700 font-semibold">
      Pedir um novo link
    </Link>
  )
}

export function ResetPasswordPage() {
  const location = useLocation()
  const navigate = useNavigate()
  const [token] = useState(() => tokenFromHash(location.hash))
  const { completePasswordReset, error } = useCompletePasswordReset()
  const [completed, setCompleted] = useState(false)

  useEffect(() => {
    if (location.hash) navigate({ hash: '' }, { replace: true })
  }, [location.hash, navigate])

  return (
    <AuthPageLayout
      title="Criar nova senha"
      subtitle="Escolha a nova senha da sua conta."
      backLink={backToLogin}
    >
      {completed ? (
        <div className="mt-8 space-y-4">
          <Alert tone="success">
            Senha redefinida! Por segurança, encerramos as sessões abertas em outros dispositivos.
          </Alert>
          <Button
            size="lg"
            className="w-full"
            leftIcon={<LogIn className="size-4" />}
            onClick={() => navigate('/login', { replace: true })}
          >
            Entrar com a nova senha
          </Button>
        </div>
      ) : !token ? (
        <Alert tone="error" className="mt-8">
          Este link de redefinição está incompleto. Abra o link exatamente como veio no e-mail ou{' '}
          <RequestNewLink />.
        </Alert>
      ) : (
        <>
          {error && (
            <Alert tone="error" className="mt-8">
              {error.displayMessage} <RequestNewLink />.
            </Alert>
          )}
          <FormComponent
            className="mt-8"
            schema={resetUserPasswordSchema}
            defaultValues={resetPasswordDefaults}
            fields={resetPasswordFields}
            onSubmit={async ({ newPassword }) => {
              const wasReset = await completePasswordReset({ token, newPassword }).then(
                () => true,
                () => false,
              )
              setCompleted(wasReset)
            }}
            submitLabel="Salvar nova senha"
            submitIcon={<KeyRound className="size-4" />}
            submitSize="lg"
            submitFullWidth
          />
        </>
      )}
    </AuthPageLayout>
  )
}
