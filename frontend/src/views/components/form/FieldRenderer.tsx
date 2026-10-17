import { useEffect, useMemo, useRef, useState } from 'react'
import type { AnyFieldApi } from '@tanstack/react-form'
import { CheckIcon, ChevronsUpDownIcon, Eye, EyeOff, ImagePlus, PlusIcon, X } from 'lucide-react'
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
import { DatePicker } from '@/views/components/ui/DatePicker'
import { Spinner } from '@/views/components/ui/feedback'
import { RequiredMark } from '@/views/components/ui/RequiredMark'
import { maskCep } from '@/lib/cepInput'
import { maskCpf } from '@/lib/cpfInput'
import { maskPhone } from '@/lib/phoneInput'
import { cn } from '@/lib/utils'
import { FieldShell } from './FieldShell'
import type { BoundFieldConfig, InputMask } from './types'

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

const inputMasks: Record<InputMask, (text: string) => string> = {
  cpf: maskCpf,
  phone: maskPhone,
  cep: maskCep,
}

function applyMask(mask: InputMask | undefined, text: string): string {
  return mask ? inputMasks[mask](text) : text
}

/** Só mostra o indicador se a busca ainda estiver em andamento depois desse tempo — evita um
 *  flash do spinner quando `onValueChange` resolve na hora (ex.: CEP incompleto). */
const LOADING_INDICATOR_DELAY_MS = 150

/** Dispara `onValueChange` e, se ele devolver uma promise, acende `onLoadingChange` enquanto ela
 *  não resolve — sem bloquear a digitação nem desabilitar o campo. Descarta o resultado de uma
 *  chamada que não é mais a mais recente (o usuário já digitou de novo por cima). */
function useAsyncValueChange(onLoadingChange: (loading: boolean) => void) {
  const requestIdRef = useRef(0)

  return (result: void | Promise<void>) => {
    if (!result || typeof result.then !== 'function') return
    const requestId = ++requestIdRef.current
    const delay = setTimeout(() => {
      if (requestIdRef.current === requestId) onLoadingChange(true)
    }, LOADING_INDICATOR_DELAY_MS)
    result.finally(() => {
      clearTimeout(delay)
      if (requestIdRef.current === requestId) onLoadingChange(false)
    })
  }
}

function PasswordVisibilityToggle({
  isVisible,
  disabled,
  onToggle,
}: {
  isVisible: boolean
  disabled?: boolean
  onToggle: () => void
}) {
  const VisibilityIcon = isVisible ? EyeOff : Eye
  return (
    <button
      type="button"
      aria-label={isVisible ? 'Ocultar senha' : 'Mostrar senha'}
      aria-pressed={isVisible}
      disabled={disabled}
      onClick={onToggle}
      className="absolute top-1/2 right-2 grid size-8 -translate-y-1/2 place-items-center rounded-lg text-stone-500 transition-colors hover:bg-stone-100 hover:text-stone-700 disabled:pointer-events-none disabled:opacity-50"
    >
      <VisibilityIcon className="size-4" />
    </button>
  )
}

function TextControl({
  field,
  id,
  config,
}: ControlProps<Extract<BoundFieldConfig, { type: 'text' | 'email' | 'password' }>>) {
  const error = fieldError(field)
  const [isLoading, setIsLoading] = useState(false)
  const trackAsyncValueChange = useAsyncValueChange(setIsLoading)
  const [isPasswordVisible, setIsPasswordVisible] = useState(false)
  const isPassword = config.type === 'password'
  const inputType = isPassword && isPasswordVisible ? 'text' : config.type

  return (
    <FieldShell
      id={id}
      label={config.label}
      hint={config.hint}
      error={error}
      required={config.required}
    >
      <div className="relative">
        <Input
          id={id}
          type={inputType}
          placeholder={config.placeholder}
          autoComplete={config.autoComplete}
          maxLength={config.maxLength}
          disabled={config.disabled}
          aria-invalid={!!error}
          aria-busy={isLoading}
          className={cn((isLoading || isPassword) && 'pr-10', isLoading && isPassword && 'pr-18')}
          value={(field.state.value as string | undefined) ?? ''}
          inputMode={config.mask ? 'numeric' : undefined}
          onChange={(e) => {
            const value = applyMask(config.mask, e.target.value)
            field.handleChange(value)
            trackAsyncValueChange(config.onValueChange?.(value, field.form))
          }}
          onBlur={field.handleBlur}
        />
        {isLoading && (
          <Spinner
            className={cn(
              'pointer-events-none absolute top-1/2 right-3.5 size-4 -translate-y-1/2',
              isPassword && 'right-11',
            )}
          />
        )}
        {isPassword && (
          <PasswordVisibilityToggle
            isVisible={isPasswordVisible}
            disabled={config.disabled}
            onToggle={() => setIsPasswordVisible((visible) => !visible)}
          />
        )}
      </div>
    </FieldShell>
  )
}

function DateControl({
  field,
  id,
  config,
}: ControlProps<Extract<BoundFieldConfig, { type: 'date' }>>) {
  const error = fieldError(field)
  return (
    <FieldShell
      id={id}
      label={config.label}
      hint={config.hint}
      error={error}
      required={config.required}
    >
      <DatePicker
        id={id}
        value={(field.state.value as string | undefined) ?? ''}
        onValueChange={(isoDate) => field.handleChange(isoDate)}
        onBlur={field.handleBlur}
        disabled={config.disabled}
        invalid={!!error}
      />
    </FieldShell>
  )
}

function NumberControl({
  field,
  id,
  config,
}: ControlProps<Extract<BoundFieldConfig, { type: 'number' }>>) {
  const error = fieldError(field)
  return (
    <FieldShell
      id={id}
      label={config.label}
      hint={config.hint}
      error={error}
      required={config.required}
    >
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

function TextareaControl({
  field,
  id,
  config,
}: ControlProps<Extract<BoundFieldConfig, { type: 'textarea' }>>) {
  const error = fieldError(field)
  return (
    <FieldShell
      id={id}
      label={config.label}
      hint={config.hint}
      error={error}
      required={config.required}
    >
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

function SelectControl({
  field,
  id,
  config,
}: ControlProps<Extract<BoundFieldConfig, { type: 'select' }>>) {
  const error = fieldError(field)
  return (
    <FieldShell
      id={id}
      label={config.label}
      hint={config.hint}
      error={error}
      required={config.required}
    >
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
    <FieldShell
      id={id}
      label={config.label}
      hint={config.hint}
      error={error}
      required={config.required}
    >
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
                'inline-flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-semibold ring-1 transition-colors ring-inset',
                'disabled:cursor-not-allowed disabled:opacity-50',
                active
                  ? 'bg-brand-600 ring-brand-600 text-white'
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
  const hasExactMatch = config.options.some(
    (option) => option.label.toLowerCase() === normalizedQuery,
  )
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
    <FieldShell
      id={id}
      label={config.label}
      hint={config.hint}
      error={error}
      required={config.required}
    >
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
              'border-input flex h-11 w-full items-center justify-between gap-2 rounded-xl border bg-white px-3.5 py-2 text-sm shadow-xs transition-[color,box-shadow] outline-none',
              'focus-visible:border-brand-500 focus-visible:ring-brand-500/10 hover:border-stone-300 focus-visible:ring-4',
              'disabled:cursor-not-allowed disabled:bg-stone-50 disabled:text-stone-500',
              'aria-invalid:border-red-300 aria-invalid:focus-visible:border-red-500 aria-invalid:focus-visible:ring-red-500/10',
              selected ? 'text-stone-900' : 'text-stone-500',
            )}
          >
            <span className="truncate">
              {selected?.label ?? config.placeholder ?? 'Selecione…'}
            </span>
            <ChevronsUpDownIcon className="size-4 shrink-0 text-stone-500" />
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
                      className={cn(
                        'text-brand-600',
                        option.value === value ? 'opacity-100' : 'opacity-0',
                      )}
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

function CheckboxControl({
  field,
  id,
  config,
}: ControlProps<Extract<BoundFieldConfig, { type: 'checkbox' }>>) {
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
            {config.required && <RequiredMark />}
          </Label>
        )}
      </div>
      {config.hint && !error && <p className="text-xs text-stone-500">{config.hint}</p>}
      {error && <p className="text-xs font-medium text-red-600">{error}</p>}
    </div>
  )
}

function ImageControl({
  field,
  id,
  config,
}: ControlProps<Extract<BoundFieldConfig, { type: 'image' }>>) {
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
    <FieldShell
      id={id}
      label={config.label}
      hint={config.hint}
      error={error}
      required={config.required}
    >
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
              'text-brand-600 hover:text-brand-700 inline-flex cursor-pointer items-center gap-1.5 text-sm font-medium transition-colors',
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
            'border-input flex h-40 w-full cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed bg-stone-50 text-stone-500 transition-colors',
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
      return <TextControl field={field} id={id} config={config} />
    case 'date':
      return <DateControl field={field} id={id} config={config} />
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
