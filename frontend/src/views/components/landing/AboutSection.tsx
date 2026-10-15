import { Reveal } from '@/views/components/common/Reveal'
import { SectionHeading } from './SectionHeading'
import { BarChart3, MapPinned, Scale, type LucideIcon } from 'lucide-react'

interface Highlight {
  icon: LucideIcon
  title: string
  description: string
}

const highlights: Highlight[] = [
  {
    icon: MapPinned,
    title: 'Encontre onde doar ou retirar',
    description:
      'Veja no mapa os pontos de coleta da sua cidade, o que cada um precisa agora e o que tem disponível, com horário e contato.',
  },
  {
    icon: Scale,
    title: 'Distribuição justa',
    description:
      'Todos os pontos do município compartilham o histórico de retiradas. Assim, cada família recebe dentro do seu limite por ciclo, sem exceder e sem ficar de fora.',
  },
  {
    icon: BarChart3,
    title: 'Transparência e rastreabilidade',
    description:
      'Estoque, metas e retiradas ficam registrados, ajudando a organização a prestar contas e a planejar o que mais falta.',
  },
]

export function AboutSection() {
  return (
    <section id="sobre" className="mx-auto w-full max-w-6xl px-4 py-16 sm:px-6 lg:py-24">
      <SectionHeading
        eyebrow="Sobre o projeto"
        title="Doação organizada chega a quem precisa"
        description="O Dooa conecta associações, igrejas e órgãos públicos em uma rede única de pontos de coleta, para que doações e retiradas sejam acompanhadas com clareza e justiça."
      />

      <ul className="mt-12 grid gap-5 md:grid-cols-3">
        {highlights.map(({ icon: Icon, title, description }, index) => (
          <li key={title}>
            <Reveal delayMilliseconds={index * 120} className="h-full">
              <article className="group shadow-card hover:shadow-lift hover:border-brand-200 relative h-full overflow-hidden rounded-3xl border border-stone-200 bg-white p-6 transition-all duration-300 hover:-translate-y-1.5">
                <span className="from-brand-400 to-brand-600 absolute inset-x-0 top-0 h-1 origin-left scale-x-0 bg-linear-to-r transition-transform duration-500 group-hover:scale-x-100" />
                <span className="bg-brand-50 text-brand-600 ring-brand-100 group-hover:bg-brand-600 grid size-12 place-items-center rounded-2xl ring-1 transition-all duration-300 ring-inset group-hover:scale-110 group-hover:-rotate-6 group-hover:text-white">
                  <Icon className="size-6" />
                </span>
                <h3 className="mt-5 text-lg font-bold text-stone-900">{title}</h3>
                <p className="mt-2 text-sm leading-relaxed text-stone-600">{description}</p>
              </article>
            </Reveal>
          </li>
        ))}
      </ul>
    </section>
  )
}
