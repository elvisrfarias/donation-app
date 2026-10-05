import { Reveal } from '@/views/components/common/Reveal'

export function SectionHeading({
  eyebrow,
  title,
  description,
}: {
  eyebrow: string
  title: string
  description?: string
}) {
  return (
    <Reveal className="mx-auto max-w-2xl text-center">
      <span className="bg-brand-50 text-brand-700 ring-brand-100 inline-flex items-center rounded-full px-3 py-1 text-xs font-bold tracking-widest uppercase ring-1 ring-inset">
        {eyebrow}
      </span>
      <h2 className="mt-4 text-3xl font-extrabold tracking-tight text-stone-900 sm:text-4xl">
        {title}
      </h2>
      {description && (
        <p className="mt-4 text-base leading-relaxed text-stone-600">{description}</p>
      )}
    </Reveal>
  )
}
