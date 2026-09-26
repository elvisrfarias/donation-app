interface FormSectionProps {
  title: string
}

/** Título de uma seção dentro de um formulário longo, sobre uma linha divisória. */
export function FormSection({ title }: FormSectionProps) {
  return (
    <div className="border-b border-stone-200 pt-2 pb-2">
      <h3 className="text-sm font-bold text-stone-900">{title}</h3>
    </div>
  )
}
