import { useState } from 'react'
import { Send } from 'lucide-react'
import { Navigate } from 'react-router-dom'
import { usePageMeta } from '@/lib/pageMeta'
import {
  forgotPasswordSchema,
  useAuth,
  useRequestPasswordReset,
  type ForgotPasswordForm,
} from '@/viewmodels'
import { AuthPageLayout } from '@/views/components/auth/AuthPageLayout'
import { FormComponent } from '@/views/components/form'
import { Alert } from '@/views/components/ui'

const forgotPasswordDefaults: ForgotPasswordForm = { email: '' }

const backToLogin = { to: '/login', label: 'Voltar para o login' }

export function ForgotPasswordPage() {
  usePageMeta({ title: 'Esqueci minha senha | Dooa', indexable: false })
  const { isAuthenticated } = useAuth()
  const { requestPasswordReset, error } = useRequestPasswordReset()
  const [sentTo, setSentTo] = useState<string | null>(null)

  if (isAuthenticated) return <Navigate to="/app" replace />

  return (
    <AuthPageLayout
      title="Esqueci minha senha"
      subtitle="Informe o e-mail da sua conta e enviaremos um link para você criar uma nova senha."
      backLink={backToLogin}
    >
      {sentTo ? (
        <Alert tone="success" className="mt-8">
          Se houver uma conta com o e-mail <strong>{sentTo}</strong>, você vai receber em instantes
          um link para redefinir a senha. Ele vale por 1 hora. Confira também a caixa de spam.
        </Alert>
      ) : (
        <>
          {error && (
            <Alert tone="error" className="mt-8">
              {error.displayMessage}
            </Alert>
          )}
          <FormComponent
            className="mt-8"
            schema={forgotPasswordSchema}
            defaultValues={forgotPasswordDefaults}
            onSubmit={async ({ email }) => {
              const wasSent = await requestPasswordReset({ email }).then(
                () => true,
                () => false,
              )
              if (wasSent) setSentTo(email)
            }}
            fields={[
              {
                name: 'email',
                type: 'email',
                label: 'E-mail',
                autoComplete: 'email',
                placeholder: 'voce@exemplo.com',
                required: true,
              },
            ]}
            submitLabel="Enviar link"
            submitIcon={<Send className="size-4" />}
            submitSize="lg"
            submitFullWidth
          />
        </>
      )}
    </AuthPageLayout>
  )
}
