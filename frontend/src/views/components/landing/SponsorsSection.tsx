import { Award, Gem, HeartHandshake, Medal, type LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import { sponsorTierLabels, sponsorTiers, type Sponsor, type SponsorTier } from '@/models/types'
import { usePublicSponsors } from '@/viewmodels'
import { Reveal } from '@/views/components/common/Reveal'
import { SectionHeading } from './SectionHeading'

interface TierStyle {
  icon: LucideIcon
  badge: string
  grid: string
  card: string
  logo: string
}

const tierStyles: Record<SponsorTier, TierStyle> = {
  Diamond: {
    icon: Gem,
    badge: 'bg-brand-50 text-brand-700 ring-brand-200',
    grid: 'grid-cols-1 sm:grid-cols-2',
    card: 'p-6 ring-brand-200 shadow-lift',
    logo: 'h-24 sm:h-28',
  },
  Gold: {
    icon: Medal,
    badge: 'bg-amber-50 text-amber-700 ring-amber-200',
    grid: 'grid-cols-2 sm:grid-cols-3',
    card: 'p-5 ring-amber-200',
    logo: 'h-16 sm:h-20',
  },
  Silver: {
    icon: Award,
    badge: 'bg-stone-100 text-stone-600 ring-stone-200',
    grid: 'grid-cols-3 sm:grid-cols-4 lg:grid-cols-5',
    card: 'p-3 ring-stone-200',
    logo: 'h-10 sm:h-12',
  },
}

function SponsorCard({ sponsor, style }: { sponsor: Sponsor; style: TierStyle }) {
  const content = (
    <>
      {sponsor.logoUrl ? (
        <img
          src={sponsor.logoUrl}
          alt={`Logo de ${sponsor.name}`}
          loading="lazy"
          className={cn('w-full object-contain', style.logo)}
        />
      ) : (
        <span className={cn('grid w-full place-items-center font-bold text-stone-700', style.logo)}>
          {sponsor.name}
        </span>
      )}
      <span className="mt-2 line-clamp-2 text-center text-xs font-semibold text-stone-600">
        {sponsor.name}
      </span>
    </>
  )

  const cardClassName = cn(
    'flex h-full flex-col items-center justify-center rounded-3xl bg-white ring-1 transition-transform duration-300',
    style.card,
  )

  return sponsor.websiteUrl ? (
    <a
      href={sponsor.websiteUrl}
      target="_blank"
      rel="sponsored noopener noreferrer"
      className={cn(cardClassName, 'hover:-translate-y-1')}
    >
      {content}
    </a>
  ) : (
    <div className={cardClassName}>{content}</div>
  )
}

function SponsorTierGroup({ tier, sponsors }: { tier: SponsorTier; sponsors: Sponsor[] }) {
  const style = tierStyles[tier]
  const Icon = style.icon

  return (
    <Reveal className="space-y-5">
      <h3 className="flex justify-center">
        <span
          className={cn(
            'inline-flex items-center gap-2 rounded-full px-4 py-1.5 text-sm font-bold ring-1 ring-inset',
            style.badge,
          )}
        >
          <Icon className="size-4" />
          {sponsorTierLabels[tier]}
        </span>
      </h3>
      <ul className={cn('mx-auto grid max-w-4xl gap-4', style.grid)}>
        {sponsors.map((sponsor) => (
          <li key={sponsor.id}>
            <SponsorCard sponsor={sponsor} style={style} />
          </li>
        ))}
      </ul>
    </Reveal>
  )
}

function BecomeSponsorInvite({ isFirst }: { isFirst: boolean }) {
  return (
    <Reveal className="mx-auto mt-12 max-w-2xl rounded-3xl border border-dashed border-stone-300 bg-white/70 p-6 text-center">
      <HeartHandshake className="text-brand-600 mx-auto size-8" />
      <p className="mt-3 text-base font-bold text-stone-900">
        {isFirst
          ? 'Sua instituição pode ser a primeira a patrocinar o Dooa'
          : 'Quer ver sua instituição aqui?'}
      </p>
      <p className="mt-2 text-sm leading-relaxed text-stone-600">
        Igrejas, empresas e associações podem patrocinar o projeto todo mês, nas categorias
        Diamante, Ouro ou Prata, e aparecer nesta seção.
      </p>
      <a
        href="#apoie"
        className="text-brand-600 hover:text-brand-700 mt-4 inline-block text-sm font-semibold"
      >
        Quero apoiar o projeto
      </a>
    </Reveal>
  )
}

/** Patrocinadores mensais do projeto, agrupados da maior para a menor categoria. */
export function SponsorsSection() {
  const { sponsors, isLoading, isError } = usePublicSponsors()

  if (isError) return null

  const groups = sponsorTiers
    .map((tier) => ({ tier, sponsors: sponsors.filter((sponsor) => sponsor.tier === tier) }))
    .filter((group) => group.sponsors.length > 0)

  return (
    <section id="patrocinadores" className="mx-auto w-full max-w-6xl px-4 py-16 sm:px-6 lg:py-24">
      <SectionHeading
        eyebrow="Patrocinadores"
        title="Quem mantém o Dooa no ar"
        description="Instituições que patrocinam o projeto todo mês e ajudam a manter o sistema gratuito para os pontos de coleta e para quem precisa."
      />

      {groups.length > 0 && (
        <div className="mt-14 space-y-12">
          {groups.map((group) => (
            <SponsorTierGroup key={group.tier} tier={group.tier} sponsors={group.sponsors} />
          ))}
        </div>
      )}

      {!isLoading && <BecomeSponsorInvite isFirst={groups.length === 0} />}
    </section>
  )
}
