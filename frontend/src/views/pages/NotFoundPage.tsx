import { Link } from 'react-router-dom'
import { Compass } from 'lucide-react'
import { Button } from '@/views/components/ui'

export function NotFoundPage() {
  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-5 bg-stone-50 p-6 text-center">
      <span className="grid size-16 place-items-center rounded-2xl bg-brand-50 text-brand-600 ring-1 ring-inset ring-brand-100">
        <Compass className="size-8" />
      </span>
      <div className="space-y-2">
        <h1 className="text-4xl font-extrabold tracking-tight text-stone-900">404</h1>
        <p className="max-w-sm text-stone-500">
          A página que você procura não existe ou foi movida.
        </p>
      </div>
      <Link to="/">
        <Button size="lg">Ir para o mapa</Button>
      </Link>
    </div>
  )
}
