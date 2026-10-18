import { Suspense, useRef, useState } from 'react'
import {
  ChevronLeft,
  ChevronRight,
  Clock,
  ExternalLink,
  HandHeart,
  HeartHandshake,
  MapPin,
  MessageSquare,
  Phone,
  Share2,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { DEFAULT_PAGE_TITLE, usePageMeta } from '@/lib/pageMeta'
import { pointPagePath, sharePoint } from '@/lib/pointActions'
import { collectionPointTypeLabels, formatAddress, type CollectionPoint } from '@/models/types'
import { useCollectionPointsMapViewModel } from '@/viewmodels/useCollectionPointsMapViewModel'
import { CollectionPointsMap } from '@/views/components/map/CollectionPointsMap'
import { pointTypeIcon, pointTypeSoftStyle } from '@/views/components/map/pointTypeStyle'
import { PledgeDonationModal, PointReviewsModal } from '@/views/components/points/lazyPointModals'
import { LandingSections } from '@/views/components/landing/LandingSections'
import { SiteHeader } from '@/views/components/landing/SiteHeader'
import { StarRating } from '@/views/components/reviews/StarRating'
import { FALLBACK_ICON, ICON_BY_KEY } from '@/views/components/stock/categoryIcons'
import { Alert, CenteredSpinner, EmptyState } from '@/views/components/ui/feedback'
import { Select } from '@/views/components/ui/Select'

const PAGE_SIZE = 5
const SIDE_BY_SIDE_LAYOUT_QUERY = '(min-width: 1024px)'

export function MapPage() {
  usePageMeta({ title: DEFAULT_PAGE_TITLE, canonicalPath: '/' })
  const vm = useCollectionPointsMapViewModel()
  const [page, setPage] = useState(0)
  const [reviewsPoint, setReviewsPoint] = useState<CollectionPoint | null>(null)
  const [pledgePoint, setPledgePoint] = useState<CollectionPoint | null>(null)
  const mapPanelRef = useRef<HTMLDivElement>(null)

  function revealMapOnSmallScreens() {
    const isStackedLayout = !window.matchMedia(SIDE_BY_SIDE_LAYOUT_QUERY).matches
    if (isStackedLayout)
      mapPanelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' })
  }

  const filterKey = JSON.stringify(vm.filters)
  const [seenFilterKey, setSeenFilterKey] = useState(filterKey)
  if (seenFilterKey !== filterKey) {
    setSeenFilterKey(filterKey)
    setPage(0)
  }

  const totalPages = Math.max(1, Math.ceil(vm.filtered.length / PAGE_SIZE))
  const currentPage = Math.min(page, totalPages - 1)
  const pageItems = vm.filtered.slice(currentPage * PAGE_SIZE, currentPage * PAGE_SIZE + PAGE_SIZE)

  return (
    <div className="bg-canvas">
      <a href="#conteudo-principal" className="skip-link">
        Pular para os pontos de doação
      </a>
      <SiteHeader />
      <main
        id="conteudo-principal"
        tabIndex={-1}
        className="mx-auto max-w-384 px-3 pt-3 outline-none sm:px-6 sm:pt-4"
      >
        <div className="animate-in fade-in slide-in-from-bottom-4 shadow-card flex h-[calc(100dvh-12rem)] min-h-136 flex-col overflow-hidden rounded-3xl border border-stone-200 bg-white lg:flex-row">
          <section className="flex w-full shrink-0 flex-col border-b border-stone-200 bg-white lg:w-md lg:border-r lg:border-b-0 xl:w-lg">
            <div className="border-b border-stone-100 p-4">
              <Select
                aria-label="Cidade"
                value={vm.filters.city}
                onValueChange={(city) => vm.setFilters({ ...vm.filters, city })}
              >
                <option value="all">Todas as cidades</option>
                {vm.cities.map((city) => (
                  <option key={city} value={city}>
                    {city}
                  </option>
                ))}
              </Select>
            </div>

            <div className="flex items-center justify-between gap-3 px-4 pt-4 pb-2">
              <div className="min-w-0">
                <h1 className="text-lg font-bold text-stone-900">Pontos de doação</h1>
                <p className="text-xs text-stone-500">
                  {vm.filtered.length} {vm.filtered.length === 1 ? 'encontrado' : 'encontrados'} ·{' '}
                  {vm.geocodingPending ? 'localizando…' : `${vm.located} no mapa`}
                </p>
              </div>
              {totalPages > 1 && (
                <div className="flex shrink-0 items-center gap-1">
                  <button
                    type="button"
                    aria-label="Página anterior"
                    disabled={currentPage <= 0}
                    onClick={() => setPage((p) => Math.max(0, p - 1))}
                    className="grid size-8 place-items-center rounded-lg border border-stone-200 text-stone-600 transition-colors hover:bg-stone-50 disabled:opacity-40 disabled:hover:bg-transparent"
                  >
                    <ChevronLeft className="size-4" />
                  </button>
                  <span className="px-1 text-xs font-medium text-stone-500 tabular-nums">
                    {currentPage + 1} / {totalPages}
                  </span>
                  <button
                    type="button"
                    aria-label="Próxima página"
                    disabled={currentPage >= totalPages - 1}
                    onClick={() => setPage((p) => Math.min(totalPages - 1, p + 1))}
                    className="grid size-8 place-items-center rounded-lg border border-stone-200 text-stone-600 transition-colors hover:bg-stone-50 disabled:opacity-40 disabled:hover:bg-transparent"
                  >
                    <ChevronRight className="size-4" />
                  </button>
                </div>
              )}
            </div>

            <div className="min-h-0 flex-1 overflow-y-auto px-4 pt-1 pb-4">
              {vm.unlocatablePointName && (
                <Alert tone="warning" className="mb-2.5">
                  Não foi possível localizar “{vm.unlocatablePointName}” no mapa. Use o endereço
                  listado ou o botão de rota.
                </Alert>
              )}
              {vm.isLoading ? (
                <CenteredSpinner label="Carregando pontos…" />
              ) : vm.isError || vm.filtered.length === 0 ? (
                <EmptyState icon={<MapPin className="size-6" />} title="Nenhum ponto encontrado" />
              ) : (
                <ul className="space-y-2.5">
                  {pageItems.map(({ item, position, distanceKm }, index) => {
                    const Icon = pointTypeIcon[item.type]
                    const selected = item.id === vm.selectedId
                    return (
                      <li
                        key={item.id}
                        className="animate-in fade-in slide-in-from-bottom-3 fill-mode-backwards duration-500"
                        style={{ animationDelay: `${index * 70}ms` }}
                      >
                        <button
                          type="button"
                          onClick={() => {
                            revealMapOnSmallScreens()
                            void vm.focusPoint(item.id)
                          }}
                          className={cn(
                            'flex w-full gap-3.5 rounded-2xl border p-3 text-left transition-all',
                            selected
                              ? 'border-brand-300 bg-brand-50 ring-brand-200 ring-1'
                              : 'hover:shadow-card border-stone-200 bg-white hover:border-stone-300',
                          )}
                        >
                          <div
                            className="grid size-16 shrink-0 place-items-center rounded-xl"
                            style={pointTypeSoftStyle(item.type)}
                          >
                            <Icon className="size-7" />
                          </div>
                          <div className="min-w-0 flex-1">
                            <p className="truncate font-bold text-stone-900">{item.name}</p>
                            <div className="mt-1.5 flex flex-wrap items-center gap-1.5">
                              <span
                                className="rounded-full px-2 py-0.5 text-[11px] font-semibold"
                                style={pointTypeSoftStyle(item.type)}
                              >
                                {collectionPointTypeLabels[item.type]}
                              </span>
                              {distanceKm != null && (
                                <span className="bg-brand-50 text-brand-600 ring-brand-100 rounded-full px-2 py-0.5 text-[11px] font-semibold ring-1 ring-inset">
                                  {distanceKm < 1
                                    ? `${Math.round(distanceKm * 1000)} m`
                                    : `${distanceKm.toFixed(1)} km`}
                                </span>
                              )}
                            </div>
                            {(item.reviewCount > 0 || item.beneficiariesServed > 0) && (
                              <div className="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1">
                                {item.reviewCount > 0 && (
                                  <span className="flex items-center gap-1.5">
                                    <StarRating value={item.averageRating} size="sm" />
                                    <span className="text-[11px] font-medium text-stone-500 tabular-nums">
                                      {item.averageRating.toFixed(1)} ({item.reviewCount})
                                    </span>
                                  </span>
                                )}
                                {item.beneficiariesServed > 0 && (
                                  <span className="inline-flex items-center gap-1 text-[11px] font-medium text-emerald-600">
                                    <HeartHandshake className="size-3.5" />
                                    {item.beneficiariesServed}{' '}
                                    {item.beneficiariesServed === 1
                                      ? 'pessoa atendida'
                                      : 'pessoas atendidas'}
                                  </span>
                                )}
                              </div>
                            )}
                            {item.description && (
                              <p className="mt-1.5 line-clamp-2 text-xs text-stone-500">
                                {item.description}
                              </p>
                            )}
                            <div className="mt-2 space-y-1">
                              <p className="flex items-center gap-1.5 text-xs text-stone-500">
                                <MapPin className="size-3.5 shrink-0 text-stone-500" />
                                <span className="truncate">{formatAddress(item.address)}</span>
                              </p>
                              <p className="flex items-center gap-1.5 text-xs text-stone-500">
                                <Phone className="size-3.5 shrink-0 text-stone-500" />
                                {item.contact.phone}
                              </p>
                              {item.openingHours && (
                                <p className="flex items-center gap-1.5 text-xs text-stone-500">
                                  <Clock className="size-3.5 shrink-0 text-stone-500" />
                                  <span className="truncate">{item.openingHours}</span>
                                </p>
                              )}
                            </div>
                            {item.categories.length > 0 && (
                              <div className="mt-2 flex flex-wrap gap-1">
                                {item.categories.map((category) => {
                                  const Icon = ICON_BY_KEY[category.icon] ?? FALLBACK_ICON
                                  return (
                                    <span
                                      key={category.id}
                                      className="inline-flex items-center gap-1 rounded-full bg-stone-100 px-2 py-0.5 text-[11px] font-medium text-stone-600 ring-1 ring-stone-200 ring-inset"
                                    >
                                      <Icon className="size-3" aria-hidden />
                                      {category.name}
                                    </span>
                                  )
                                })}
                              </div>
                            )}
                            {item.needs.length > 0 && (
                              <div className="mt-2">
                                <p className="text-[10px] font-bold tracking-wide text-amber-600 uppercase">
                                  Precisa agora
                                </p>
                                <div className="mt-1 flex flex-wrap gap-1">
                                  {item.needs.map((need) => (
                                    <span
                                      key={need}
                                      className="rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-700 ring-1 ring-amber-200 ring-inset"
                                    >
                                      {need}
                                    </span>
                                  ))}
                                </div>
                              </div>
                            )}
                            {item.stockItems.length > 0 && (
                              <div className="mt-2">
                                <p className="text-[10px] font-bold tracking-wide text-emerald-600 uppercase">
                                  Disponível agora
                                </p>
                                <div className="mt-1 flex flex-wrap gap-1">
                                  {item.stockItems.map((s) => (
                                    <span
                                      key={s.id}
                                      className="rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-700 ring-1 ring-emerald-200 ring-inset"
                                    >
                                      {s.name}: {s.quantity}
                                    </span>
                                  ))}
                                </div>
                              </div>
                            )}
                            {!position && (
                              <p className="mt-1.5 inline-flex items-center gap-1.5 text-[11px] text-amber-600">
                                <span className="size-1.5 rounded-full bg-amber-500" />
                                {vm.geocodingPending ? 'Localizando…' : 'Não localizado no mapa'}
                              </p>
                            )}
                          </div>
                        </button>
                        <div className="mt-1 flex flex-wrap items-center justify-end gap-1">
                          <Link
                            to={pointPagePath(item)}
                            className="inline-flex min-h-11 items-center gap-1.5 rounded-lg px-2 py-1 text-xs font-semibold text-stone-600 transition-colors hover:bg-stone-100 sm:min-h-0"
                          >
                            <ExternalLink className="size-3.5" aria-hidden />
                            Página do ponto
                          </Link>
                          <button
                            type="button"
                            onClick={() => sharePoint(item)}
                            className="inline-flex min-h-11 items-center gap-1.5 rounded-lg px-2 py-1 text-xs font-semibold text-stone-500 transition-colors hover:bg-stone-100 sm:min-h-0"
                          >
                            <Share2 className="size-3.5" />
                            Compartilhar
                          </button>
                          <button
                            type="button"
                            onClick={() => setPledgePoint(item)}
                            className="inline-flex min-h-11 items-center gap-1.5 rounded-lg px-2 py-1 text-xs font-semibold text-emerald-700 transition-colors hover:bg-emerald-50 sm:min-h-0"
                          >
                            <HandHeart className="size-3.5" aria-hidden />
                            Quero doar
                          </button>
                          <button
                            type="button"
                            onClick={() => setReviewsPoint(item)}
                            className="text-brand-600 hover:bg-brand-50 inline-flex min-h-11 items-center gap-1.5 rounded-lg px-2 py-1 text-xs font-semibold transition-colors sm:min-h-0"
                          >
                            <MessageSquare className="size-3.5" />
                            {item.reviewCount > 0
                              ? `Ver ${item.reviewCount} ${item.reviewCount === 1 ? 'avaliação' : 'avaliações'}`
                              : 'Avaliar ponto'}
                          </button>
                        </div>
                      </li>
                    )
                  })}
                </ul>
              )}
            </div>
          </section>

          <div ref={mapPanelRef} className="relative min-h-64 flex-1">
            <CollectionPointsMap
              points={vm.filtered}
              selectedId={vm.selectedId}
              onSelect={vm.setSelectedId}
              focusRequest={vm.focusRequest}
              userLocation={vm.userLocation}
              userAccuracyMeters={vm.locationAccuracyMeters}
              onPickLocation={vm.locationIsApproximate ? vm.pickLocation : undefined}
            />
          </div>
        </div>
      </main>

      <LandingSections />

      <Suspense fallback={null}>
        {reviewsPoint && (
          <PointReviewsModal point={reviewsPoint} onClose={() => setReviewsPoint(null)} />
        )}
        {pledgePoint && (
          <PledgeDonationModal point={pledgePoint} onClose={() => setPledgePoint(null)} />
        )}
      </Suspense>
    </div>
  )
}
