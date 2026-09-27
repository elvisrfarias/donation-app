import { useMemo, useState, type ChangeEvent } from 'react'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { Input } from './inputs'

const DIACRITICS = /[̀-ͯ]/g

function normalize(text: string): string {
  return text.normalize('NFD').replace(DIACRITICS, '').toLowerCase()
}

interface ComboBoxInputProps {
  id?: string
  'aria-label'?: string
  value: string
  onValueChange: (value: string) => void
  suggestions: string[]
  disabled?: boolean
  invalid?: boolean
  placeholder?: string
  maxLength?: number
}

/**
 * Campo de texto livre com sugestões num painel no mesmo estilo do `Select` — substitui o
 * `<input list>` nativo (datalist), cujo menu não segue o visual do app.
 */
export function ComboBoxInput({
  id,
  'aria-label': ariaLabel,
  value,
  onValueChange,
  suggestions,
  disabled,
  invalid,
  placeholder,
  maxLength,
}: ComboBoxInputProps) {
  const [open, setOpen] = useState(false)

  const filtered = useMemo(() => {
    const term = normalize(value.trim())
    return term.length === 0
      ? suggestions
      : suggestions.filter((option) => normalize(option).includes(term))
  }, [value, suggestions])

  const showList = open && filtered.length > 0

  function choose(option: string) {
    onValueChange(option)
    setOpen(false)
  }

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    onValueChange(event.target.value)
    setOpen(true)
  }

  return (
    <Popover open={showList} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Input
          id={id}
          type="text"
          role="combobox"
          aria-label={ariaLabel}
          aria-expanded={showList}
          aria-autocomplete="list"
          autoComplete="off"
          value={value}
          maxLength={maxLength}
          disabled={disabled}
          invalid={invalid}
          placeholder={placeholder}
          onChange={handleChange}
        />
      </PopoverTrigger>
      <PopoverContent
        align="start"
        className="w-(--radix-popover-trigger-width) min-w-40 p-1"
        onOpenAutoFocus={(event) => event.preventDefault()}
      >
        <ul className="max-h-56 space-y-0.5 overflow-y-auto">
          {filtered.map((option) => (
            <li key={option}>
              <button
                type="button"
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => choose(option)}
                className="w-full rounded-lg px-2.5 py-2 text-left text-sm text-stone-700 hover:bg-brand-50 hover:text-brand-700"
              >
                {option}
              </button>
            </li>
          ))}
        </ul>
      </PopoverContent>
    </Popover>
  )
}
