import { Reveal } from '@/views/components/common/Reveal'
import { SectionHeading } from './SectionHeading'

interface Step {
  title: string
  description: string
}

const steps: Step[] = [
  {
    title: 'Encontre um ponto',
    description:
      'Use o mapa e o filtro de cidade para achar o ponto de coleta mais próximo de você.',
  },
  {
    title: 'Doe ou retire',
    description:
      'Confira o que o ponto precisa e o que tem em estoque, e entre em contato ou vá até lá no horário de atendimento.',
  },
  {
    title: 'Tudo fica registrado',
    description:
      'Cada retirada entra no histórico compartilhado, garantindo equidade entre todos os pontos da cidade.',
  },
  {
    title: 'Avalie e acompanhe',
    description: 'Deixe sua avaliação do ponto e ajude a comunidade a saber onde a ajuda funciona.',
  },
]

export function HowItWorksSection() {
  return (
    <section id="como-funciona" className="border-y border-stone-200 bg-white">
      <div className="mx-auto w-full max-w-6xl px-4 py-16 sm:px-6 lg:py-24">
        <SectionHeading
          eyebrow="Como funciona"
          title="Simples para quem doa, justo para quem recebe"
        />

        <ol className="relative mt-14 grid gap-10 sm:grid-cols-2 lg:grid-cols-4 lg:gap-6">
          <span
            aria-hidden
            className="via-brand-200 absolute top-5 right-[12.5%] left-[12.5%] hidden h-px bg-linear-to-r from-transparent to-transparent lg:block"
          />
          {steps.map((step, index) => (
            <li key={step.title}>
              <Reveal delayMilliseconds={index * 140} className="group relative text-center">
                <span className="from-brand-400 to-brand-600 shadow-brand relative mx-auto grid size-11 place-items-center rounded-2xl bg-linear-to-br text-lg font-extrabold text-white transition-transform duration-300 group-hover:scale-110 group-hover:-rotate-6">
                  {index + 1}
                </span>
                <h3 className="mt-5 text-base font-bold text-stone-900">{step.title}</h3>
                <p className="mx-auto mt-2 max-w-60 text-sm leading-relaxed text-stone-600">
                  {step.description}
                </p>
              </Reveal>
            </li>
          ))}
        </ol>
      </div>
    </section>
  )
}
