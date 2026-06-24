import type { ReactNode } from 'react'
import type { AnyFormApi } from '@tanstack/react-form'

export interface SelectOption {
  value: string
  label: ReactNode
  disabled?: boolean
}

export interface AutoCompleteOption {
  value: string
  label: string
}

interface BaseField {
  /** Caminho do campo no schema (chave do objeto de valores). */
  name: string
  label?: string
  hint?: string
  required?: boolean
  disabled?: boolean
  /**
   * Largura do campo na grade do formulário. `'full'` ocupa a linha inteira
   * (em qualquer nº de colunas); `2` ocupa duas colunas; `1` (padrão) ocupa uma.
   */
  colSpan?: 1 | 2 | 'full'
}

type TextLikeField = BaseField & {
  type: 'text' | 'email' | 'password' | 'date'
  placeholder?: string
  autoComplete?: string
  maxLength?: number
}

type NumberField = BaseField & {
  type: 'number'
  placeholder?: string
  min?: number
  max?: number
  step?: number
}

type TextareaField = BaseField & {
  type: 'textarea'
  placeholder?: string
  rows?: number
}

type SelectField = BaseField & {
  type: 'select'
  options: SelectOption[]
  placeholder?: string
}

/** Seleção múltipla renderizada como chips alternáveis; vinculada a um `string[]`. */
type MultiSelectField = BaseField & {
  type: 'multiselect'
  options: SelectOption[]
}

type AutoCompleteField = BaseField & {
  type: 'autocomplete'
  options: AutoCompleteOption[]
  placeholder?: string
  searchPlaceholder?: string
  emptyText?: string
}

type CheckboxField = BaseField & { type: 'checkbox' }

type ImageField = BaseField & {
  type: 'image'
  accept?: string
  /** URL de uma imagem já existente (ex.: edição). Serve de pré-visualização
   * inicial enquanto nenhum arquivo novo é selecionado. */
  previewUrl?: string | null
}

/** Escape hatch: conteúdo arbitrário com acesso à API do formulário. */
type CustomField = {
  type: 'custom'
  key: string
  colSpan?: 1 | 2 | 'full'
  render: (form: AnyFormApi) => ReactNode
}

/** Campos vinculados a um valor do schema (tudo menos `custom`). */
export type BoundFieldConfig =
  | TextLikeField
  | NumberField
  | TextareaField
  | SelectField
  | MultiSelectField
  | AutoCompleteField
  | CheckboxField
  | ImageField

export type FormFieldConfig = BoundFieldConfig | CustomField
