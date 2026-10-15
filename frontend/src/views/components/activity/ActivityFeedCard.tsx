import { Bell, Boxes, HeartHandshake, Megaphone, Star, type LucideIcon } from 'lucide-react'
import { formatDateTime } from '@/lib/utils'
import type { ActivityKind } from '@/models/types'
import { useActivityFeed } from '@/viewmodels'
import { Alert, Card, CenteredSpinner } from '@/views/components/ui'

const kindPresentation: Record<ActivityKind, { icon: LucideIcon; label: string; tone: string }> = {
  Donation: { icon: HeartHandshake, label: 'Doação', tone: 'bg-emerald-50 text-emerald-600' },
  Notice: { icon: Megaphone, label: 'Aviso', tone: 'bg-brand-50 text-brand-600' },
  Review: { icon: Star, label: 'Avaliação', tone: 'bg-amber-50 text-amber-600' },
  LowStock: { icon: Boxes, label: 'Estoque', tone: 'bg-red-50 text-red-600' },
}

/**
 * Mecanismo de percepção do ponto: junta doações prometidas, avisos, avaliações e alertas
 * de estoque num único feed, para o gestor saber o que mudou sem abrir cada tela.
 */
export function ActivityFeedCard({ pointId }: { pointId: string | null }) {
  const feed = useActivityFeed(pointId)

  return (
    <Card className="p-6">
      <div className="flex items-center gap-2">
        <Bell className="text-brand-600 size-5" aria-hidden />
        <h2 className="text-base font-semibold text-stone-900">Atividade recente</h2>
      </div>
      <p className="mt-1 text-sm text-stone-500">O que mudou no seu ponto. Atualiza sozinho.</p>

      <div aria-live="polite" className="mt-4">
        {feed.isLoading && <CenteredSpinner label="Carregando atividade…" />}
        {feed.isError && <Alert tone="error">{feed.error?.displayMessage}</Alert>}
        {!feed.isLoading && !feed.isError && feed.items.length === 0 && (
          <p className="text-sm text-stone-500">Nada de novo por aqui ainda.</p>
        )}
        {feed.items.length > 0 && (
          <ul className="divide-y divide-stone-100">
            {feed.items.map((item) => {
              const presentation = kindPresentation[item.kind]
              return (
                <li
                  key={`${item.kind}-${item.occurredAtUtc}-${item.title}`}
                  className="flex gap-3 py-3"
                >
                  <span
                    className={`grid size-9 shrink-0 place-items-center rounded-xl ${presentation.tone}`}
                  >
                    <presentation.icon className="size-4" aria-hidden />
                    <span className="sr-only">{presentation.label}</span>
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-semibold text-stone-900">{item.title}</p>
                    <p className="line-clamp-2 text-sm text-stone-500">{item.description}</p>
                  </div>
                  <time
                    dateTime={item.occurredAtUtc}
                    className="shrink-0 text-xs text-stone-500 tabular-nums"
                  >
                    {formatDateTime(item.occurredAtUtc)}
                  </time>
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </Card>
  )
}
