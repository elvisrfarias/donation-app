import type { ReactNode } from 'react'
import { ArrowLeft, MapPin, ShieldCheck, Users } from 'lucide-react'
import { Link } from 'react-router-dom'
import { AppLogo } from '@/views/components/common/AppLogo'

const highlights = [
  { icon: MapPin, text: 'Pontos de coleta no mapa do OpenStreetMap' },
  { icon: Users, text: 'Rastreabilidade de beneficiários' },
  { icon: ShieldCheck, text: 'Limite de retiradas por ciclo, no município inteiro' },
]

interface Props {
  title: string
  subtitle: string
  children: ReactNode
  backLink?: { to: string; label: string }
}

/** Moldura das telas de acesso (login, esqueci minha senha, redefinição): painel da marca + formulário. */
export function AuthPageLayout({
  title,
  subtitle,
  children,
  backLink = { to: '/', label: 'Voltar ao mapa público' },
}: Props) {
  return (
    <div className="grid min-h-dvh lg:grid-cols-2">
      <div className="glow-brand bg-sidebar-deep relative hidden flex-col justify-between overflow-hidden p-12 text-white lg:flex">
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
          <h2 className="max-w-md text-4xl leading-[1.1] font-extrabold tracking-tight">
            Gestão de doações com <span className="text-brand-200">equidade</span> entre todos os
            pontos.
          </h2>
          <ul className="space-y-4 text-sm text-white/80">
            {highlights.map(({ icon: Icon, text }) => (
              <li key={text} className="flex items-center gap-3">
                <span className="grid size-9 shrink-0 place-items-center rounded-2xl bg-white/10 text-white ring-1 ring-white/15 ring-inset">
                  <Icon className="size-4.5" />
                </span>
                {text}
              </li>
            ))}
          </ul>
        </div>

        <p className="relative text-xs text-white/50">© Dooa — controle de equidade municipal.</p>
      </div>

      <div className="bg-canvas flex items-center justify-center p-6">
        <div className="w-full max-w-sm">
          <AppLogo className="mb-10 lg:hidden" />
          <h1 className="text-3xl font-bold text-stone-900">{title}</h1>
          <p className="mt-2 text-sm text-stone-500">{subtitle}</p>

          {children}

          <p className="mt-8 text-center text-sm text-stone-500">
            <Link
              to={backLink.to}
              className="hover:text-brand-600 inline-flex items-center gap-1.5 font-semibold text-stone-600 transition-colors"
            >
              <ArrowLeft className="size-4" />
              {backLink.label}
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
