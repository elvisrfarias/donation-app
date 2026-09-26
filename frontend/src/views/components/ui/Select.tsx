import {
  Children,
  isValidElement,
  useRef,
  useState,
  type ComponentProps,
  type ReactNode,
} from 'react'
import { Check, ChevronDown } from 'lucide-react'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { cn } from '@/lib/utils'
import { inputBase, stateRing } from './fieldStyles'

const SEARCHABLE_OPTIONS_THRESHOLD = 8
const DIACRITICS = /[̀-ͯ]/g

interface SelectOption {
  value: string
  label: string
  disabled: boolean
}

interface SelectProps {
  id?: string
  'aria-label'?: string
  value: string
  onValueChange: (value: string) => void
  children: ReactNode
  placeholder?: string
  disabled?: boolean
  invalid?: boolean
  className?: string
}

function normalize(text: string): string {
  return text.normalize('NFD').replace(DIACRITICS, '').toLowerCase()
}

function matchesSearch(_value: string, search: string, keywords?: string[]): number {
  return normalize((keywords ?? []).join(' ')).includes(normalize(search)) ? 1 : 0
}

function readOptions(children: ReactNode): SelectOption[] {
  return Children.toArray(children)
    .filter(isValidElement<ComponentProps<'option'>>)
    .map((element) => ({
      value: String(element.props.value ?? ''),
      label: Children.toArray(element.props.children).join(''),
      disabled: !!element.props.disabled,
    }))
}

export function Select({
  id,
  'aria-label': ariaLabel,
  value,
  onValueChange,
  children,
  placeholder,
  disabled,
  invalid,
  className,
}: SelectProps) {
  const [open, setOpen] = useState(false)
  const [highlighted, setHighlighted] = useState(value)
  const commandRef = useRef<HTMLDivElement>(null)

  const allOptions = readOptions(children)
  const emptyOption = allOptions.find((option) => option.value === '')
  const options = allOptions.filter((option) => option.value !== '')
  const selected = options.find((option) => option.value === value)
  const emptyLabel = placeholder ?? emptyOption?.label ?? 'Selecione…'
  const isSearchable = options.length > SEARCHABLE_OPTIONS_THRESHOLD

  function handleOpenChange(nextOpen: boolean) {
    if (nextOpen) setHighlighted(value)
    setOpen(nextOpen)
  }

  function choose(nextValue: string) {
    onValueChange(nextValue)
    setOpen(false)
  }

  return (
    <Popover open={open} onOpenChange={handleOpenChange}>
      <PopoverTrigger asChild>
        <button
          id={id}
          type="button"
          role="combobox"
          aria-label={ariaLabel}
          aria-expanded={open}
          aria-invalid={invalid || undefined}
          disabled={disabled}
          className={cn(
            inputBase,
            'flex h-11 items-center justify-between gap-2 text-left',
            stateRing(invalid),
            className,
          )}
        >
          <span className={cn('truncate', !selected && 'text-stone-400')}>
            {selected?.label ?? emptyLabel}
          </span>
          <ChevronDown
            className={cn(
              'size-4 shrink-0 text-stone-400 transition-transform',
              open && 'rotate-180',
            )}
          />
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        className="w-(--radix-popover-trigger-width) min-w-44 p-0"
        onOpenAutoFocus={(event) => {
          if (isSearchable) return
          event.preventDefault()
          commandRef.current?.focus()
        }}
      >
        <Command
          ref={commandRef}
          filter={matchesSearch}
          value={highlighted}
          onValueChange={setHighlighted}
        >
          {isSearchable && <CommandInput placeholder="Buscar…" />}
          <CommandList>
            <CommandEmpty>Nenhum resultado</CommandEmpty>
            <CommandGroup>
              {options.map((option) => (
                <CommandItem
                  key={option.value}
                  value={option.value}
                  keywords={[option.label]}
                  disabled={option.disabled}
                  onSelect={() => choose(option.value)}
                  className={cn(option.value === value && 'font-medium text-stone-900')}
                >
                  <span className="flex-1">{option.label}</span>
                  {option.value === value && <Check className="text-brand-600" />}
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
