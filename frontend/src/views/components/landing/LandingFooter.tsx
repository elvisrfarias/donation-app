import { Link } from 'react-router-dom'
import { AppLogo } from '@/views/components/common/AppLogo'

export function LandingFooter() {
  return (
    <footer className="border-t border-stone-200 bg-white">
      <div className="mx-auto flex w-full max-w-6xl flex-col items-center justify-between gap-4 px-4 py-8 sm:flex-row sm:px-6">
        <AppLogo />
        <nav className="flex items-center gap-5 text-sm font-medium text-stone-600">
          <a href="#sobre" className="hover:text-brand-600">
            Sobre
          </a>
          <a href="#como-funciona" className="hover:text-brand-600">
            Como funciona
          </a>
          <a href="#apoie" className="hover:text-brand-600">
            Apoiar
          </a>
          <Link to="/login" className="hover:text-brand-600">
            Entrar
          </Link>
        </nav>
        <p className="text-xs text-stone-500">© {new Date().getFullYear()} Bondade</p>
      </div>
    </footer>
  )
}
