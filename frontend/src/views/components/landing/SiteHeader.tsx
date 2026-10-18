import { LogIn } from 'lucide-react'
import { Link } from 'react-router-dom'
import { AppLogo } from '@/views/components/common/AppLogo'

const navLinks = [
  { href: '/#sobre', label: 'Sobre' },
  { href: '/#como-funciona', label: 'Como funciona' },
  { href: '/#patrocinadores', label: 'Patrocinadores' },
  { href: '/#apoie', label: 'Apoiar' },
]

/** Cabeçalho das páginas públicas (mapa e página de cada ponto). */
export function SiteHeader() {
  return (
    <header className="animate-in fade-in slide-in-from-top-4 sticky top-0 z-1050 border-b border-stone-200/80 bg-white/85 backdrop-blur duration-500">
      <div className="mx-auto flex h-16 max-w-384 items-center justify-between gap-4 px-4 sm:px-6">
        <Link to="/" title="Início">
          <AppLogo />
        </Link>
        <nav className="hidden items-center gap-1 md:flex">
          {navLinks.map((link) => (
            <a
              key={link.href}
              href={link.href}
              className="hover:bg-brand-50 hover:text-brand-700 inline-flex min-h-11 items-center rounded-full px-4 text-sm font-semibold text-stone-600 transition-colors"
            >
              {link.label}
            </a>
          ))}
        </nav>
        <Link
          to="/login"
          className="from-brand-500 to-brand-700 shadow-brand hover:shadow-lift inline-flex h-11 items-center gap-2 rounded-full bg-linear-to-r px-5 text-sm font-semibold text-white transition-all"
        >
          <LogIn className="size-4" />
          Entrar
        </Link>
      </div>
    </header>
  )
}
