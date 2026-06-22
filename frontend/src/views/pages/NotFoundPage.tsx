import { Link } from 'react-router-dom'
import { Compass } from 'lucide-react'

export function NotFoundPage() {
  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-4 bg-slate-50 p-6 text-center">
      <span className="grid size-16 place-items-center rounded-2xl bg-brand-50 text-brand-600">
        <Compass className="size-8" />
      </span>
      <h1 className="text-3xl font-bold text-slate-900">404</h1>
      <p className="max-w-sm text-slate-500">A página que você procura não existe ou foi movida.</p>
      <Link
        to="/"
        className="inline-flex h-10 items-center rounded-lg bg-brand-600 px-5 text-sm font-medium text-white hover:bg-brand-700"
      >
        Ir para o mapa
      </Link>
    </div>
  )
}
