import { useEffect, useMemo, useState } from 'react'
import type { AnyFieldApi } from '@tanstack/react-form'
import { CheckIcon, ChevronsUpDownIcon, ImagePlus, PlusIcon, X } from 'lucide-react'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Command,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import { FieldShell } from './FieldShell'
import type { BoundFieldConfig } from './types'

/** Primeira mensagem de erro do campo, exibida só após interação ou tentativa de submit. */
function fieldError(field: AnyFieldApi): string | undefined {
  const show = field.state.meta.isTouched || field.form.state.submissionAttempts > 0
  if (!show) return undefined
  const err = field.state.meta.errors[0] as { message?: string } | string | undefined
  if (err == null) return undefined
  return typeof err === 'string' ? err : err.message
}

interface ControlProps<TConfig extends BoundFieldConfig> {
  field: AnyFieldApi
  id: string
  config: TConfig
}

function TextControl({
  field,
  id,
  config,
}: ControlProps<Extract<BoundFieldConfig, { type: 'text' | 'email' | 'password' | 'date' }>>) {
  const error = fieldError(field)
  return (
    <FieldShell id={id} label={config.label} hint={config.hint} error={error} required={config.required}>
      <Input
        id={id}
        type={config.type}
        placeholder={config.placeholder}
        autoComplete={config.autoComplete}
        maxLength={config.maxLength}
        disabled={config.disabled}
        aria-invalid={!!error}
        value={(field.state.value as string | undefined) ?? ''}
        onChange={(e) => field.handleChange(e.target.value)}
        onBlur={field.handleBlur}
      />
    </FieldShell>
  )
}

function NumberControl({ field, id, config }: ControlProps<Extract<BoundFieldConfig, { type: 'number' }>>) {
  const error = fieldError(field)
  return (
    <FieldShell id={id} label={config.label} hint={config.hint} error={error} required={config.required}>
      <Input
        id={id}
        type="number"
        inputMode="numeric"
        placeholder={config.placeholder}
        min={config.min}
        max={config.max}
        step={config.step}
        disabled={config.disabled}
        aria-invalid={!!error}
        value={(field.state.value as number | undefined) ?? ''}
        onChange={(e) =>
          field.handleChange(e.target.value === '' ? undefined : e.target.valueAsNumber)
        }
        onBlur={field.handleBlur}
      />
    </FieldShell>
  )
}

function TextareaControl({ field, id, config }: ControlProps<Extract<BoundFieldConfig, { type: 'textarea' }>>) {
  const error = fieldError(field)
  return (
    <FieldShell id={id} label={config.label} hint={config.hint} error={error} required={config.required}>
      <Textarea
        id={id}
        placeholder={config.placeholder}
        rows={config.rows}
        disabled={config.disabled}
        aria-invalid={!!error}
        value={(field.state.value as string | undefined) ?? ''}
        onChange={(e) => field.handleChange(e.target.value)}
        onBlur={field.handleBlur}
      />
    </FieldShell>
  )
}

function SelectControl({ field, id, config }: ControlProps<Extract<BoundFieldConfig, { type: 'select' }>>) {
  const error = fieldError(field)
  return (
    <FieldShell id={id} label={config.label} hint={config.hint} error={error} required={config.required}>
      <Select
        value={(field.state.value as string | undefined) ?? ''}
        onValueChange={(v) => field.handleChange(v)}
        disabled={config.disabled}
      >
        <SelectTrigger id={id} aria-invalid={!!error} onBlur={field.handleBlur}>
          <SelectValue placeholder={config.placeholder ?? 'Selecione…'} />
        </SelectTrigger>
        <SelectContent>
          {config.options.map((option) => (
            <SelectItem key={option.value} value={option.value} disabled={option.disabled}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </FieldShell>
  )
}

function MultiSelectControl({
  field,
  id,
  config,
}: ControlProps<Extract<BoundFieldConfig, { type: 'multiselect' }>>) {
  const error = fieldError(field)
  const selected = (field.state.value as string[] | undefined) ?? []

  function toggle(value: string) {
    const next = selected.includes(value)
      ? selected.filter((v) => v !== value)
      : [...selected, value]
    field.handleChange(next)
    field.handleBlur()
  }

  return (
    <FieldShell id={id} label={config.label} hint={config.hint} error={error} required={config.required}>
      <div className="flex flex-wrap gap-2" role="group" aria-invalid={!!error}>
        {config.options.map((option) => {
          const active = selected.includes(option.value)
          return (
            <button
              key={option.value}
              type="button"
              aria-pressed={active}
              disabled={config.disabled || option.disabled}
              onClick={() => toggle(option.value)}
              className={cn(
                'inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-semibold ring-1 ring-inset transition-colors',
                'disabled:cursor-not-allowed disabled:opacity-50',
                active
                  ? 'bg-brand-600 text-white ring-brand-600'
                  : 'bg-white text-stone-600 ring-stone-200 hover:bg-stone-50',
              )}
            >
              {active && <CheckIcon className="size-3.5" />}
              {option.label}
            </button>
          )
        })}
      </div>
    </FieldShell>
  )
}

function AutoCompleteControl({
  field,
  id,
  config,
}: ControlProps<Extract<BoundFieldConfig, { type: 'autocomplete' }>>) {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [creating, setCreating] = useState(false)
  const error = fieldError(field)
  const value = field.state.value as string | undefined
  const selected = config.options.find((option) => option.value === value)

  const normalizedQuery = query.trim().toLowerCase()
  const filteredOptions = normalizedQuery
    ? config.options.filter((option) => option.label.toLowerCase().includes(normalizedQuery))
    : config.options
  const hasExactMatch = config.options.some((option) => option.label.toLowerCase() === normalizedQuery)
  const showCreate = !!config.onCreate && normalizedQuery.length > 0 && !hasExactMatch

  function changeOpen(next: boolean) {
    setOpen(next)
    if (!next) setQuery('')
  }

  async function handleCreate() {
    if (!config.onCreate) return
    setCreating(true)
    try {
      const created = await config.onCreate(query.trim())
      if (created) field.handleChange(created)
    } finally {
      setCreating(false)
      changeOpen(false)
    }
  }

  return (
    <FieldShell id={id} label={config.label} hint={config.hint} error={error} required={config.required}>
      <Popover open={open} onOpenChange={changeOpen}>
        <PopoverTrigger asChild>
          <button
            type="button"
            id={id}
            role="combobox"
            aria-expanded={open}
            aria-invalid={!!error}
            disabled={config.disabled}
            onBlur={field.handleBlur}
            className={cn(
              'flex h-11 w-full items-center justify-between gap-2 rounded-xl border border-input bg-white px-3.5 py-2 text-sm shadow-xs transition-[color,box-shadow] outline-none',
              'hover:border-stone-300 focus-visible:border-brand-500 focus-visible:ring-4 focus-visible:ring-brand-500/10',
              'disabled:cursor-not-allowed disabled:bg-stone-50 disabled:text-stone-400',
              'aria-invalid:border-red-300 aria-invalid:focus-visible:border-red-500 aria-invalid:focus-visible:ring-red-500/10',
              selected ? 'text-stone-900' : 'text-stone-400',
            )}
          >
            <span className="truncate">{selected?.label ?? config.placeholder ?? 'Selecione…'}</span>
            <ChevronsUpDownIcon className="size-4 shrink-0 text-stone-400" />
          </button>
        </PopoverTrigger>
        <PopoverContent className="w-(--radix-popover-trigger-width) p-0" align="start">
          <Command shouldFilter={false}>
            <CommandInput
              value={query}
              onValueChange={setQuery}
              placeholder={config.searchPlaceholder ?? 'Buscar…'}
            />
            <CommandList>
              {filteredOptions.length === 0 && !showCreate && (
                <div className="py-6 text-center text-sm text-stone-500">
                  {config.emptyText ?? 'Nada encontrado.'}
                </div>
              )}
              <CommandGroup>
                {filteredOptions.map((option) => (
                  <CommandItem
                    key={option.value}
                    value={option.value}
                    onSelect={() => {
                      field.handleChange(option.value)
                      changeOpen(false)
                    }}
                  >
                    <CheckIcon
                      className={cn('text-brand-600', option.value === value ? 'opacity-100' : 'opacity-0')}
                    />
                    {option.label}
                  </CommandItem>
                ))}
                {showCreate && (
                  <CommandItem
                    key="__create__"
                    value="__create__"
                    disabled={creating}
                    onSelect={handleCreate}
                    className="text-brand-700"
                  >
                    <PlusIcon className="text-brand-600" />
                    {config.createLabel?.(query.trim()) ?? `Criar “${query.trim()}”`}
                  </CommandItem>
                )}
              </CommandGroup>
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>
    </FieldShell>
  )
}

function CheckboxControl({ field, id, config }: ControlProps<Extract<BoundFieldConfig, { type: 'checkbox' }>>) {
  const error = fieldError(field)
  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-center gap-2.5">
        <Checkbox
          id={id}
          checked={!!field.state.value}
          onCheckedChange={(checked) => field.handleChange(checked === true)}
          onBlur={field.handleBlur}
          disabled={config.disabled}
          aria-invalid={!!error}
        />
        {config.label && (
          <Label htmlFor={id} className="font-medium">
            {config.label}
            {config.required && <span className="text-brand-600">*</span>}
          </Label>
        )}
      </div>
      {config.hint && !error && <p className="text-xs text-stone-500">{config.hint}</p>}
      {error && <p className="text-xs font-medium text-red-600">{error}</p>}
    </div>
  )
}

function ImageControl({ field, id, config }: ControlProps<Extract<BoundFieldConfig, { type: 'image' }>>) {
  const error = fieldError(field)
  const value: unknown = field.state.value
  const file = value instanceof File ? value : undefined

  const filePreview = useMemo(() => (file ? URL.createObjectURL(file) : null), [file])
  useEffect(() => {
    if (!filePreview) return
    return () => URL.revokeObjectURL(filePreview)
  }, [filePreview])

  const previewUrl = filePreview ?? config.previewUrl ?? null

  return (
    <FieldShell id={id} label={config.label} hint={config.hint} error={error} required={config.required}>
      {previewUrl ? (
        <div className="space-y-2">
          <div className="relative w-fit">
            <img
              src={previewUrl}
              alt="Pré-visualização"
              className="h-40 w-64 rounded-xl object-cover ring-1 ring-stone-200"
            />
            {file && (
              <button
                type="button"
                onClick={() => field.handleChange(undefined)}
                aria-label="Remover imagem"
                className="absolute -top-2 -right-2 grid size-7 place-items-center rounded-full bg-stone-900/80 text-white transition-colors hover:bg-stone-900"
              >
                <X className="size-4" />
              </button>
            )}
          </div>
          <label
            htmlFor={id}
            className={cn(
              'inline-flex cursor-pointer items-center gap-1.5 text-sm font-medium text-brand-600 transition-colors hover:text-brand-700',
              config.disabled && 'pointer-events-none opacity-50',
            )}
          >
            <ImagePlus className="size-4" />
            Trocar imagem
          </label>
        </div>
      ) : (
        <label
          htmlFor={id}
          className={cn(
            'flex h-40 w-full cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed border-input bg-stone-50 text-stone-500 transition-colors',
            'hover:border-brand-300 hover:text-brand-600',
            config.disabled && 'pointer-events-none opacity-50',
          )}
        >
          <ImagePlus className="size-6" />
          <span className="text-sm font-medium">Selecionar imagem</span>
        </label>
      )}
      <input
        key={file ? 'filled' : 'empty'}
        id={id}
        type="file"
        accept={config.accept ?? 'image/jpeg,image/png,image/webp,image/gif,image/avif'}
        disabled={config.disabled}
        className="sr-only"
        onBlur={field.handleBlur}
        onChange={(e) => field.handleChange(e.target.files?.[0])}
      />
    </FieldShell>
  )
}

/** Renderiza o controle adequado para o campo, ligado à API do TanStack Form. */
export function FieldRenderer({ field, config }: { field: AnyFieldApi; config: BoundFieldConfig }) {
  const id = `field-${config.name}`
  switch (config.type) {
    case 'text':
    case 'email':
    case 'password':
    case 'date':
      return <TextControl field={field} id={id} config={config} />
    case 'number':
      return <NumberControl field={field} id={id} config={config} />
    case 'textarea':
      return <TextareaControl field={field} id={id} config={config} />
    case 'select':
      return <SelectControl field={field} id={id} config={config} />
    case 'multiselect':
      return <MultiSelectControl field={field} id={id} config={config} />
    case 'autocomplete':
      return <AutoCompleteControl field={field} id={id} config={config} />
    case 'checkbox':
      return <CheckboxControl field={field} id={id} config={config} />
    case 'image':
      return <ImageControl field={field} id={id} config={config} />
  }
}
