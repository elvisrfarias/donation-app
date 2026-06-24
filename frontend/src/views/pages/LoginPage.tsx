import { ArrowLeft, ArrowRight, MapPin, ShieldCheck, Users } from 'lucide-react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { loginSchema, useAuth, useLoginViewModel } from '@/viewmodels'
import { AppLogo } from '@/views/components/common/AppLogo'
import { FormComponent } from '@/views/components/form'
import { Alert } from '@/views/components/ui'

interface LocationState {
  from?: string
}

const highlights = [
  { icon: MapPin, text: 'Pontos de coleta no mapa do OpenStreetMap' },
  { icon: Users, text: 'Rastreabilidade familiar e de beneficiários' },
  { icon: ShieldCheck, text: 'Limite de retiradas por ciclo, no município inteiro' },
]

export function LoginPage() {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { login, error } = useLoginViewModel()

  if (isAuthenticated) return <Navigate to="/app" replace />

  const from = (location.state as LocationState | null)?.from ?? '/app'

  return (
    <div className="grid min-h-dvh lg:grid-cols-2">
      <div className="glow-brand relative hidden flex-col justify-between overflow-hidden bg-sidebar-deep p-12 text-white lg:flex">
        <div
          className="pointer-events-none absolute inset-0 opacity-[0.04]"
          style={{
            backgroundImage:
              'linear-gradient(to right, white 1px, transparent 1px), linear-gradient(to bottom, white 1px, transparent 1px)',
            backgroundSize: '40px 40px',
          }}
        />
        <AppLogo inverted />

        <div className="relative space-y-8">
          <h2 className="max-w-md text-4xl font-extrabold leading-[1.1] tracking-tight">
            Gestão de doações com{' '}
            <span className="text-brand-200">equidade</span> entre todos os pontos.
          </h2>
          <ul className="space-y-4 text-sm text-white/80">
            {highlights.map(({ icon: Icon, text }) => (
              <li key={text} className="flex items-center gap-3">
                <span className="grid size-9 shrink-0 place-items-center rounded-2xl bg-white/10 text-white ring-1 ring-inset ring-white/15">
                  <Icon className="size-4.5" />
                </span>
                {text}
              </li>
            ))}
          </ul>
        </div>

        <p className="relative text-xs text-white/50">
          © Doação App — controle de equidade municipal.
        </p>
      </div>

      <div className="flex items-center justify-center bg-canvas p-6">
        <div className="w-full max-w-sm">
          <AppLogo className="mb-10 lg:hidden" />
          <h1 className="text-3xl font-bold text-stone-900">Acesse sua conta</h1>
          <p className="mt-2 text-sm text-stone-500">Área de gestão dos pontos de coleta.</p>

          {error && <Alert tone="error" className="mt-8">{error.displayMessage}</Alert>}

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

          <p className="mt-8 text-center text-sm text-stone-500">
            <Link
              to="/"
              className="inline-flex items-center gap-1.5 font-semibold text-stone-600 transition-colors hover:text-brand-600"
            >
              <ArrowLeft className="size-4" />
              Voltar ao mapa público
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
