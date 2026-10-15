import type { ReactNode } from 'react'
import { CircleHelp } from 'lucide-react'

/** Ajuda contextual recolhível: explica a tela em linguagem simples, sem ocupar espaço por padrão. */
export function HowItWorks({ children }: { children: ReactNode }) {
  return (
    <details className="group rounded-2xl bg-white p-4 ring-1 ring-stone-200 ring-inset">
      <summary className="flex min-h-11 cursor-pointer items-center gap-2 text-sm font-semibold text-stone-700">
        <CircleHelp className="text-brand-600 size-4" aria-hidden />
        Como esta tela funciona
      </summary>
      <p className="mt-2 text-sm leading-relaxed text-stone-600">{children}</p>
    </details>
  )
}
