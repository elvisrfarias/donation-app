import { Link } from 'react-router-dom'
import { Compass } from 'lucide-react'
import { usePageMeta } from '@/lib/pageMeta'
import { Button } from '@/views/components/ui/Button'

export function NotFoundPage() {
  usePageMeta({ title: 'Página não encontrada | Dooa', indexable: false })

  return (
    <div className="bg-canvas flex min-h-dvh flex-col items-center justify-center gap-5 p-6 text-center">
      <span className="from-brand-500 to-brand-700 shadow-brand grid size-16 place-items-center rounded-3xl bg-linear-to-br text-white">
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
