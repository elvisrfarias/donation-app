import { Megaphone } from 'lucide-react'
import { formatDate } from '@/lib/utils'
import { usePublicNotices } from '@/viewmodels'

/** Avisos do mural de um ponto para a comunidade (ex.: "precisamos de leite em pó"). */
export function PublicNoticesList({ pointId }: { pointId: string }) {
  const { notices } = usePublicNotices(pointId)

  if (notices.length === 0) return null

  return (
    <section aria-label="Avisos do ponto" className="space-y-2">
      <h3 className="flex items-center gap-2 text-sm font-semibold text-stone-800">
        <Megaphone className="text-brand-600 size-4" aria-hidden />
        Avisos do ponto
      </h3>
      <ul className="space-y-2">
        {notices.map((notice) => (
          <li
            key={notice.id}
            className="bg-brand-50/60 ring-brand-100 rounded-2xl p-3.5 ring-1 ring-inset"
          >
            <div className="flex items-start justify-between gap-3">
              <p className="font-semibold text-stone-900">{notice.title}</p>
              <time dateTime={notice.createdAtUtc} className="shrink-0 text-xs text-stone-500">
                {formatDate(notice.createdAtUtc)}
              </time>
            </div>
            <p className="mt-1 text-sm whitespace-pre-line text-stone-600">{notice.message}</p>
          </li>
        ))}
      </ul>
    </section>
  )
}
