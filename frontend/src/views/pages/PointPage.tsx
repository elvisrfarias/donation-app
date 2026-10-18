import { Suspense, useState, type ReactNode } from 'react'
import {
  ArrowLeft,
  Clock,
  HandHeart,
  HeartHandshake,
  Mail,
  MapPin,
  MessageSquare,
  Navigation,
  Phone,
  Share2,
} from 'lucide-react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { usePageMeta } from '@/lib/pageMeta'
import { pointDirectionsUrl, pointPagePath, sharePoint } from '@/lib/pointActions'
import { collectionPointTypeLabels, formatAddress, type CollectionPoint } from '@/models/types'
import { usePublicCollectionPoint } from '@/viewmodels/usePublicCollectionPointViewModel'
import { LandingFooter } from '@/views/components/landing/LandingFooter'
import { SiteHeader } from '@/views/components/landing/SiteHeader'
import { pointTypeIcon, pointTypeSoftStyle } from '@/views/components/map/pointTypeStyle'
import { PledgeDonationModal, PointReviewsModal } from '@/views/components/points/lazyPointModals'
import { StarRating } from '@/views/components/reviews/StarRating'
import { FALLBACK_ICON, ICON_BY_KEY } from '@/views/components/stock/categoryIcons'
import { Button } from '@/views/components/ui/Button'
import { CenteredSpinner } from '@/views/components/ui/feedback'
import { NotFoundPage } from './NotFoundPage'

const MAX_META_DESCRIPTION_LENGTH = 160

function metaDescriptionOf(point: CollectionPoint): string {
  const intro =
    point.description?.trim() ||
    `Ponto de doação e coleta em ${point.address.city}/${point.address.state}.`
  const needs = point.needs.length > 0 ? ` Precisa agora: ${point.needs.join(', ')}.` : ''
  const text = `${intro} Endereço: ${formatAddress(point.address)}.${needs}`
  return text.length <= MAX_META_DESCRIPTION_LENGTH
    ? text
    : `${text.slice(0, MAX_META_DESCRIPTION_LENGTH - 1).trimEnd()}…`
}

function InfoRow({ icon, children }: { icon: ReactNode; children: ReactNode }) {
  return (
    <li className="flex items-start gap-3 text-sm text-stone-700">
      <span className="mt-0.5 shrink-0 text-stone-500">{icon}</span>
      <span className="min-w-0">{children}</span>
    </li>
  )
}

function ChipList({
  title,
  tone,
  items,
}: {
  title: string
  tone: 'amber' | 'emerald' | 'stone'
  items: { key: string; label: ReactNode }[]
}) {
  const toneClasses = {
    amber: 'bg-amber-50 text-amber-800 ring-amber-200',
    emerald: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
    stone: 'bg-stone-100 text-stone-700 ring-stone-200',
  }[tone]

  return (
    <section>
      <h2 className="text-sm font-bold text-stone-900">{title}</h2>
      <ul className="mt-2 flex flex-wrap gap-1.5">
        {items.map((item) => (
          <li
            key={item.key}
            className={`inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-medium ring-1 ring-inset ${toneClasses}`}
          >
            {item.label}
          </li>
        ))}
      </ul>
    </section>
  )
}

function PointDetails({ point }: { point: CollectionPoint }) {
  const [reviewsOpen, setReviewsOpen] = useState(false)
  const [pledgeOpen, setPledgeOpen] = useState(false)
  const TypeIcon = pointTypeIcon[point.type]
  const place = `${point.address.city}/${point.address.state}`

  usePageMeta({
    title: `${point.name} — ponto de doação em ${place} | Dooa`,
    description: metaDescriptionOf(point),
    canonicalPath: pointPagePath(point),
  })

  return (
    <>
      <main id="conteudo-principal" className="mx-auto w-full max-w-5xl px-4 pt-6 pb-16 sm:px-6">
        <Link
          to="/"
          className="hover:text-brand-600 inline-flex min-h-11 items-center gap-1.5 text-sm font-semibold text-stone-600"
        >
          <ArrowLeft className="size-4" />
          Todos os pontos de doação
        </Link>

        <article className="shadow-card mt-2 overflow-hidden rounded-3xl border border-stone-200 bg-white">
          {point.imageUrl && (
            <img
              src={point.imageUrl}
              alt={`Foto de ${point.name}`}
              className="aspect-[21/9] w-full bg-stone-100 object-cover"
            />
          )}

          <div className="grid gap-8 p-6 sm:p-8 lg:grid-cols-[1fr_20rem]">
            <div className="min-w-0 space-y-6">
              <header className="space-y-3">
                <span
                  className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold"
                  style={pointTypeSoftStyle(point.type)}
                >
                  <TypeIcon className="size-3.5" aria-hidden />
                  {collectionPointTypeLabels[point.type]}
                </span>
                <h1 className="text-3xl font-extrabold tracking-tight text-stone-900 sm:text-4xl">
                  {point.name}
                </h1>
                <p className="text-sm text-stone-600">Ponto de doação e coleta em {place}</p>
                {(point.reviewCount > 0 || point.beneficiariesServed > 0) && (
                  <div className="flex flex-wrap items-center gap-x-4 gap-y-1">
                    {point.reviewCount > 0 && (
                      <span className="flex items-center gap-1.5">
                        <StarRating value={point.averageRating} size="sm" />
                        <span className="text-xs font-medium text-stone-600 tabular-nums">
                          {point.averageRating.toFixed(1)} ({point.reviewCount}{' '}
                          {point.reviewCount === 1 ? 'avaliação' : 'avaliações'})
                        </span>
                      </span>
                    )}
                    {point.beneficiariesServed > 0 && (
                      <span className="inline-flex items-center gap-1 text-xs font-medium text-emerald-700">
                        <HeartHandshake className="size-3.5" aria-hidden />
                        {point.beneficiariesServed}{' '}
                        {point.beneficiariesServed === 1 ? 'pessoa atendida' : 'pessoas atendidas'}
                      </span>
                    )}
                  </div>
                )}
              </header>

              {point.description && (
                <p className="text-base leading-relaxed whitespace-pre-line text-stone-700">
                  {point.description}
                </p>
              )}

              {point.needs.length > 0 && (
                <ChipList
                  title="Precisa agora"
                  tone="amber"
                  items={point.needs.map((need) => ({ key: need, label: need }))}
                />
              )}

              {point.categories.length > 0 && (
                <ChipList
                  title="Recebe doações de"
                  tone="stone"
                  items={point.categories.map((category) => {
                    const Icon = ICON_BY_KEY[category.icon] ?? FALLBACK_ICON
                    return {
                      key: category.id,
                      label: (
                        <>
                          <Icon className="size-3.5" aria-hidden />
                          {category.name}
                        </>
                      ),
                    }
                  })}
                />
              )}

              {point.stockItems.length > 0 && (
                <ChipList
                  title="Disponível agora"
                  tone="emerald"
                  items={point.stockItems.map((item) => ({
                    key: item.id,
                    label: `${item.name}: ${item.quantity}`,
                  }))}
                />
              )}
            </div>

            <aside className="space-y-5 lg:border-l lg:border-stone-100 lg:pl-8">
              <ul className="space-y-3">
                <InfoRow icon={<MapPin className="size-4" />}>
                  {formatAddress(point.address)}
                </InfoRow>
                {point.openingHours && (
                  <InfoRow icon={<Clock className="size-4" />}>{point.openingHours}</InfoRow>
                )}
                <InfoRow icon={<Phone className="size-4" />}>
                  <a
                    href={`tel:+55${point.contact.phone.replace(/\D/g, '')}`}
                    className="hover:text-brand-600 font-medium"
                  >
                    {point.contact.phone}
                  </a>
                </InfoRow>
                <InfoRow icon={<Mail className="size-4" />}>
                  <a
                    href={`mailto:${point.contact.email}`}
                    className="hover:text-brand-600 font-medium break-all"
                  >
                    {point.contact.email}
                  </a>
                </InfoRow>
              </ul>

              <div className="grid gap-2">
                <Button
                  variant="brand"
                  size="lg"
                  leftIcon={<HandHeart className="size-4" />}
                  onClick={() => setPledgeOpen(true)}
                >
                  Quero doar
                </Button>
                <a
                  href={pointDirectionsUrl(point)}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex h-11 items-center justify-center gap-2 rounded-full border border-stone-200 bg-white px-5 text-sm font-semibold text-stone-700 transition-colors hover:bg-stone-50"
                >
                  <Navigation className="size-4" />
                  Como chegar
                </a>
                <Button
                  variant="secondary"
                  leftIcon={<MessageSquare className="size-4" />}
                  onClick={() => setReviewsOpen(true)}
                >
                  {point.reviewCount > 0 ? 'Ver avaliações' : 'Avaliar ponto'}
                </Button>
                <Button
                  variant="ghost"
                  leftIcon={<Share2 className="size-4" />}
                  onClick={() => sharePoint(point)}
                >
                  Compartilhar
                </Button>
              </div>
            </aside>
          </div>
        </article>
      </main>

      <Suspense fallback={null}>
        {reviewsOpen && <PointReviewsModal point={point} onClose={() => setReviewsOpen(false)} />}
        {pledgeOpen && <PledgeDonationModal point={point} onClose={() => setPledgeOpen(false)} />}
      </Suspense>
    </>
  )
}

/** Página pública de um ponto de coleta: a que o Google indexa e que vai nos compartilhamentos. */
export function PointPage() {
  const { pointId = '', slug } = useParams()
  const { point, isLoading, isNotFound, isError } = usePublicCollectionPoint(pointId)

  if (isNotFound) return <NotFoundPage />

  if (point && slug !== point.slug) return <Navigate to={pointPagePath(point)} replace />

  return (
    <div className="bg-canvas flex min-h-dvh flex-col">
      <SiteHeader />
      <div className="flex-1">
        {point ? (
          <PointDetails point={point} />
        ) : isLoading ? (
          <CenteredSpinner label="Carregando ponto…" />
        ) : (
          isError && (
            <p className="mx-auto max-w-5xl px-6 py-16 text-center text-stone-600">
              Não foi possível carregar este ponto agora. Tente de novo em instantes.
            </p>
          )
        )}
      </div>
      <LandingFooter />
    </div>
  )
}
