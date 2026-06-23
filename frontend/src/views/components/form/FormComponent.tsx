import type { ReactNode } from 'react'
import { type AnyFieldApi, type AnyFormApi, useForm } from '@tanstack/react-form'
import type { z } from 'zod'
import { cn } from '@/lib/utils'
import { Button } from '@/views/components/ui'
import { FieldRenderer } from './FieldRenderer'
import type { FormFieldConfig } from './types'

interface FormComponentProps<TValues extends Record<string, unknown>> {
  /** Schema zod (Standard Schema) usado para validar o formulário inteiro. */
  schema: z.ZodType<TValues>
  defaultValues: TValues
  /** Chamado apenas quando o formulário é válido. Recebe os valores e a API do form. */
  onSubmit: (value: TValues, form: AnyFormApi) => void | Promise<void>
  fields: FormFieldConfig[]
  /** Layout em 1 ou 2 colunas (padrão: 1). */
  columns?: 1 | 2
  submitLabel: string
  submitIcon?: ReactNode
  submitSize?: 'sm' | 'md' | 'lg'
  /** Desabilita o submit por regra externa (ex.: limite atingido). */
  submitDisabled?: boolean
  submitFullWidth?: boolean
  /** Reseta o formulário aos valores padrão após um submit bem-sucedido. */
  resetOnSubmit?: boolean
  className?: string
}

/**
 * Formulário reutilizável e dirigido por configuração, sobre o TanStack Form.
 * Descreva os campos em `fields` e o componente cuida de binding, validação
 * (via schema zod) e submit. Casos especiais usam campos `type: 'custom'`.
 */
export function FormComponent<TValues extends Record<string, unknown>>({
  schema,
  defaultValues,
  onSubmit,
  fields,
  columns = 1,
  submitLabel,
  submitIcon,
  submitSize = 'md',
  submitDisabled,
  submitFullWidth,
  resetOnSubmit,
  className,
}: FormComponentProps<TValues>) {
  const form = useForm({
    defaultValues,
    // zod v4 é Standard Schema; o cast contorna a variância de input (unknown vs TValues).
    validators: { onChange: schema as never, onSubmit: schema as never },
    onSubmit: async ({ value, formApi }) => {
      await onSubmit(value, formApi as unknown as AnyFormApi)
      if (resetOnSubmit) formApi.reset()
    },
  })

  const spanFull = columns === 2 ? 'sm:col-span-2' : undefined

  return (
    <form
      noValidate
      onSubmit={(e) => {
        e.preventDefault()
        e.stopPropagation()
        void form.handleSubmit()
      }}
      className={cn('grid gap-4', columns === 2 && 'sm:grid-cols-2', className)}
    >
      {fields.map((cfg) => {
        const span = cfg.colSpan === 2 ? 'sm:col-span-2' : undefined

        if (cfg.type === 'custom') {
          return (
            <div key={cfg.key} className={span}>
              {cfg.render(form as unknown as AnyFormApi)}
            </div>
          )
        }

        return (
          <div key={cfg.name} className={span}>
            <form.Field name={cfg.name as never}>
              {(field) => <FieldRenderer field={field as unknown as AnyFieldApi} config={cfg} />}
            </form.Field>
          </div>
        )
      })}

      <form.Subscribe selector={(s) => ({ canSubmit: s.canSubmit, isSubmitting: s.isSubmitting })}>
        {({ canSubmit, isSubmitting }) => (
          <Button
            type="submit"
            size={submitSize}
            loading={isSubmitting}
            disabled={submitDisabled || !canSubmit}
            leftIcon={submitIcon}
            className={cn(submitFullWidth ? 'w-full' : 'justify-self-start', spanFull)}
          >
            {submitLabel}
          </Button>
        )}
      </form.Subscribe>
    </form>
  )
}
