import { zodResolver } from '@hookform/resolvers/zod'
import { LogIn, MapPin, ShieldCheck, Users } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { loginSchema, useAuth, useLoginViewModel, type LoginForm } from '@/viewmodels'
import { AppLogo } from '@/views/components/common/AppLogo'
import { Alert, Button, FormField, Input } from '@/views/components/ui'

interface LocationState {
  from?: string
}

export function LoginPage() {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const { login, isPending, error } = useLoginViewModel()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  if (isAuthenticated) return <Navigate to="/app" replace />

  const from = (location.state as LocationState | null)?.from ?? '/app'

  const onSubmit = handleSubmit(async (values) => {
    try {
      await login(values)
      navigate(from, { replace: true })
    } catch {
      /* erro tratado e exibido via `error` */
    }
  })

  return (
    <div className="grid min-h-dvh lg:grid-cols-2">
      <div className="relative hidden flex-col justify-between overflow-hidden bg-gradient-to-br from-brand-600 to-accent-600 p-10 text-white lg:flex">
        <AppLogo className="[&_p]:!text-white [&_span:last-child]:!text-white/80" />
        <div className="space-y-6">
          <h2 className="max-w-sm text-3xl font-bold leading-tight">
            Gestão de doações com equidade entre todos os pontos.
          </h2>
          <ul className="space-y-3 text-sm text-white/90">
            <li className="flex items-center gap-3">
              <MapPin className="size-5" /> Pontos de coleta no mapa do OpenStreetMap
            </li>
            <li className="flex items-center gap-3">
              <Users className="size-5" /> Rastreabilidade familiar e beneficiários
            </li>
            <li className="flex items-center gap-3">
              <ShieldCheck className="size-5" /> Limite de retiradas por ciclo, no município inteiro
            </li>
          </ul>
        </div>
        <p className="text-xs text-white/70">© Doação App — controle de equidade municipal.</p>
      </div>

      <div className="flex items-center justify-center p-6">
        <div className="w-full max-w-sm">
          <AppLogo className="mb-8 lg:hidden" />
          <h1 className="text-2xl font-bold text-slate-900">Acesse sua conta</h1>
          <p className="mt-1 text-sm text-slate-500">Área de gestão dos pontos de coleta.</p>

          <form onSubmit={onSubmit} className="mt-6 space-y-4" noValidate>
            {error && <Alert tone="error">{error.displayMessage}</Alert>}

            <FormField label="E-mail" htmlFor="email" error={errors.email?.message} required>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                placeholder="voce@exemplo.com"
                invalid={!!errors.email}
                {...register('email')}
              />
            </FormField>

            <FormField label="Senha" htmlFor="password" error={errors.password?.message} required>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                placeholder="••••••••"
                invalid={!!errors.password}
                {...register('password')}
              />
            </FormField>

            <Button
              type="submit"
              size="lg"
              loading={isPending}
              className="w-full"
              leftIcon={<LogIn className="size-4" />}
            >
              Entrar
            </Button>
          </form>

          <p className="mt-6 text-center text-sm text-slate-500">
            <Link to="/" className="font-medium text-brand-600 hover:underline">
              ← Voltar ao mapa público
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
