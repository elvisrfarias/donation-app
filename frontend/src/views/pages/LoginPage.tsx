import { ArrowRight } from 'lucide-react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { loginSchema, useAuth, useLoginViewModel } from '@/viewmodels'
import { AuthPageLayout } from '@/views/components/auth/AuthPageLayout'
import { FormComponent } from '@/views/components/form'
import { Alert } from '@/views/components/ui'

interface LocationState {
  from?: string
}

export function LoginPage() {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { login, error, sessionExpiredMessage } = useLoginViewModel()

  if (isAuthenticated) return <Navigate to="/app" replace />

  const from = (location.state as LocationState | null)?.from ?? '/app'

  return (
    <AuthPageLayout title="Acesse sua conta" subtitle="Área de gestão dos pontos de coleta.">
      {!error && sessionExpiredMessage && (
        <Alert tone="warning" className="mt-8">
          {sessionExpiredMessage}
        </Alert>
      )}
      {error && (
        <Alert tone="error" className="mt-8">
          {error.displayMessage}
        </Alert>
      )}

      <FormComponent
        className="mt-8"
        schema={loginSchema}
        defaultValues={{ email: '', password: '' }}
        onSubmit={async (values) => {
          await login(values)
          navigate(from, { replace: true })
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
          {
            name: 'password',
            type: 'password',
            label: 'Senha',
            autoComplete: 'current-password',
            placeholder: '••••••••',
            required: true,
          },
        ]}
        submitLabel="Entrar"
        submitIcon={<ArrowRight className="size-4" />}
        submitSize="lg"
        submitFullWidth
      />

      <p className="mt-4 text-center text-sm">
        <Link
          to="/forgot-password"
          className="text-brand-600 hover:text-brand-700 font-semibold transition-colors"
        >
          Esqueci minha senha
        </Link>
      </p>
    </AuthPageLayout>
  )
}
