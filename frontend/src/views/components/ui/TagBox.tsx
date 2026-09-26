import { useRef, useState, type KeyboardEvent, type MouseEvent, type ReactNode } from 'react'
import { Check, ChevronDown, X } from 'lucide-react'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'
import { inputBase, stateRing } from './fieldStyles'

export interface TagBoxOption {
  value: string
  label: string
  icon?: ReactNode
}

interface TagBoxProps {
  id?: string
  'aria-label'?: string
  options: TagBoxOption[]
  value: string[]
  onValueChange: (value: string[]) => void
  placeholder?: string
  disabled?: boolean
  invalid?: boolean
  className?: string
}

export function TagBox({
  id,
  'aria-label': ariaLabel,
  options,
  value,
  onValueChange,
  placeholder = 'Selecione…',
  disabled,
  invalid,
  className,
}: TagBoxProps) {
  const [open, setOpen] = useState(false)
  const commandRef = useRef<HTMLDivElement>(null)
  const selected = options.filter((option) => value.includes(option.value))

  function toggle(optionValue: string) {
    onValueChange(
      value.includes(optionValue)
        ? value.filter((item) => item !== optionValue)
        : options
            .filter((option) => [...value, optionValue].includes(option.value))
            .map((o) => o.value),
    )
  }

  function remove(event: MouseEvent, optionValue: string) {
    event.stopPropagation()
    onValueChange(value.filter((item) => item !== optionValue))
  }

  function openOnKeyboard(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault()
      setOpen(true)
    }
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <div
          id={id}
          role="combobox"
          aria-label={ariaLabel}
          aria-expanded={open}
          aria-invalid={invalid || undefined}
          aria-disabled={disabled || undefined}
          tabIndex={disabled ? -1 : 0}
          onKeyDown={openOnKeyboard}
          className={cn(
            inputBase,
            'flex min-h-11 cursor-pointer flex-wrap items-center gap-1.5 py-1.5 pr-9 text-left',
            'relative',
            stateRing(invalid),
            disabled && 'pointer-events-none opacity-60',
            className,
          )}
        >
          {selected.length === 0 && <span className="text-stone-400">{placeholder}</span>}
          {selected.map((option) => (
            <span
              key={option.value}
              className="bg-brand-50 text-brand-700 ring-brand-100 inline-flex items-center gap-1 rounded-full py-0.5 pr-1 pl-2 text-xs font-semibold ring-1 ring-inset"
            >
              {option.icon}
              {option.label}
              <button
                type="button"
                aria-label={`Remover ${option.label}`}
                onClick={(event) => remove(event, option.value)}
                className="hover:bg-brand-100 grid size-4 place-items-center rounded-full"
              >
                <X className="size-3" />
              </button>
            </span>
          ))}
          <ChevronDown
            className={cn(
              'absolute top-1/2 right-3 size-4 -translate-y-1/2 text-stone-400 transition-transform',
              open && 'rotate-180',
            )}
          />
        </div>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        className="w-(--radix-popover-trigger-width) min-w-52 p-0"
        onOpenAutoFocus={(event) => {
          event.preventDefault()
          commandRef.current?.focus()
        }}
      >
        <Command ref={commandRef}>
          <CommandList>
            <CommandEmpty>Nenhum resultado</CommandEmpty>
            <CommandGroup>
              {options.map((option) => {
                const isSelected = value.includes(option.value)
                return (
                  <CommandItem
                    key={option.value}
                    value={option.value}
                    keywords={[option.label]}
                    onSelect={() => toggle(option.value)}
                    className={cn(isSelected && 'font-medium text-stone-900')}
                  >
                    {option.icon}
                    <span className="flex-1">{option.label}</span>
                    {isSelected && <Check className="text-brand-600" />}
                  </CommandItem>
                )
              })}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
