import { useState } from 'react'
import { CalendarDays, ChevronLeft, ChevronRight, X } from 'lucide-react'
import { DayPicker } from 'react-day-picker'
import { ptBR } from 'react-day-picker/locale'
import 'react-day-picker/style.css'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import {
  brazilianToIso,
  dateToIso,
  isoToBrazilian,
  isoToDate,
  maskBrazilianDate,
} from '@/lib/dateInput'
import { cn } from '@/lib/utils'
import { Button } from './Button'
import { inputBase, stateRing } from './fieldStyles'
import { Select } from './Select'

const OLDEST_SELECTABLE_YEAR = 1900
const FUTURE_YEARS_WHEN_ALLOWED = 10
const MONTH_NAMES = Array.from({ length: 12 }, (_, month) =>
  new Intl.DateTimeFormat('pt-BR', { month: 'long' }).format(new Date(2000, month, 1)),
)

interface DatePickerProps {
  id?: string
  value: string
  onValueChange: (isoDate: string) => void
  onBlur?: () => void
  disabled?: boolean
  invalid?: boolean
  allowFuture?: boolean
  className?: string
}

function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1)
}

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate())
}

export function DatePicker({
  id,
  value,
  onValueChange,
  onBlur,
  disabled,
  invalid,
  allowFuture = false,
  className,
}: DatePickerProps) {
  const today = startOfDay(new Date())
  const lastSelectableYear = today.getFullYear() + (allowFuture ? FUTURE_YEARS_WHEN_ALLOWED : 0)
  const selectedDate = isoToDate(value)

  const [text, setText] = useState(isoToBrazilian(value))
  const [emittedValue, setEmittedValue] = useState(value)
  const [syncedValue, setSyncedValue] = useState(value)
  const [open, setOpen] = useState(false)
  const [visibleMonth, setVisibleMonth] = useState(selectedDate ?? today)

  if (value !== syncedValue) {
    setSyncedValue(value)
    if (value !== emittedValue) {
      setText(isoToBrazilian(value))
      setEmittedValue(value)
    }
  }

  function isSelectable(date: Date): boolean {
    return allowFuture || date <= today
  }

  function commit(isoDate: string) {
    setEmittedValue(isoDate)
    if (isoDate !== value) onValueChange(isoDate)
  }

  function handleTextChange(rawText: string) {
    const maskedText = maskBrazilianDate(rawText)
    setText(maskedText)
    const isoDate = brazilianToIso(maskedText)
    const date = isoDate ? isoToDate(isoDate) : undefined
    const isAccepted = isoDate !== null && date !== undefined && isSelectable(date)
    commit(isAccepted ? isoDate : '')
    if (isAccepted && date) setVisibleMonth(date)
  }

  function handleBlur() {
    if (text !== '' && text !== isoToBrazilian(value)) setText(isoToBrazilian(value))
    onBlur?.()
  }

  function handleOpenChange(nextOpen: boolean) {
    if (nextOpen) setVisibleMonth(selectedDate ?? today)
    setOpen(nextOpen)
  }

  function chooseDate(date: Date) {
    setText(isoToBrazilian(dateToIso(date)))
    commit(dateToIso(date))
    setOpen(false)
  }

  function clear() {
    setText('')
    commit('')
    setOpen(false)
  }

  function showMonth(monthIndex: number, year: number) {
    setVisibleMonth(new Date(year, monthIndex, 1))
  }

  function shiftMonth(step: number) {
    showMonth(visibleMonth.getMonth() + step, visibleMonth.getFullYear())
  }

  const textIsInvalid = text.length === 10 && value === ''
  const years = Array.from(
    { length: lastSelectableYear - OLDEST_SELECTABLE_YEAR + 1 },
    (_, index) => lastSelectableYear - index,
  )
  const isAtOldestMonth =
    visibleMonth.getFullYear() === OLDEST_SELECTABLE_YEAR && visibleMonth.getMonth() === 0
  const isAtLatestMonth =
    visibleMonth.getFullYear() === lastSelectableYear && visibleMonth.getMonth() === 11

  return (
    <Popover open={open} onOpenChange={handleOpenChange}>
      <div className="relative">
        <input
          id={id}
          type="text"
          inputMode="numeric"
          autoComplete="off"
          placeholder="dd/mm/aaaa"
          maxLength={10}
          value={text}
          disabled={disabled}
          aria-invalid={invalid || textIsInvalid || undefined}
          onChange={(event) => handleTextChange(event.target.value)}
          onBlur={handleBlur}
          className={cn(
            inputBase,
            'h-11 pr-20 tabular-nums',
            stateRing(invalid || textIsInvalid),
            className,
          )}
        />
        <div className="absolute inset-y-0 right-1.5 flex items-center gap-0.5">
          {value !== '' && !disabled && (
            <button
              type="button"
              aria-label="Limpar data"
              onClick={clear}
              className="grid size-8 place-items-center rounded-lg text-stone-500 transition-colors hover:bg-stone-100 hover:text-stone-700"
            >
              <X className="size-4" />
            </button>
          )}
          <PopoverTrigger asChild>
            <button
              type="button"
              aria-label="Abrir calendário"
              disabled={disabled}
              className="hover:bg-brand-50 hover:text-brand-700 grid size-8 place-items-center rounded-lg text-stone-500 transition-colors disabled:opacity-40 disabled:hover:bg-transparent"
            >
              <CalendarDays className="size-4.5" />
            </button>
          </PopoverTrigger>
        </div>
      </div>

      <PopoverContent align="end" className="date-picker w-[21rem] p-3">
        <div className="mb-2 flex items-center gap-1.5">
          <button
            type="button"
            aria-label="Mês anterior"
            disabled={isAtOldestMonth}
            onClick={() => shiftMonth(-1)}
            className="hover:bg-brand-50 hover:text-brand-700 grid size-9 shrink-0 place-items-center rounded-xl text-stone-600 ring-1 ring-stone-200 transition-colors ring-inset disabled:opacity-40"
          >
            <ChevronLeft className="size-4" />
          </button>
          <Select
            aria-label="Mês"
            value={String(visibleMonth.getMonth())}
            onValueChange={(month) => showMonth(Number(month), visibleMonth.getFullYear())}
            className="h-9 min-w-0 flex-1 px-3"
          >
            {MONTH_NAMES.map((name, monthIndex) => (
              <option key={name} value={String(monthIndex)}>
                {capitalize(name)}
              </option>
            ))}
          </Select>
          <Select
            aria-label="Ano"
            value={String(visibleMonth.getFullYear())}
            onValueChange={(year) => showMonth(visibleMonth.getMonth(), Number(year))}
            className="h-9 w-22 shrink-0 px-3"
          >
            {years.map((year) => (
              <option key={year} value={String(year)}>
                {year}
              </option>
            ))}
          </Select>
          <button
            type="button"
            aria-label="Próximo mês"
            disabled={isAtLatestMonth}
            onClick={() => shiftMonth(1)}
            className="hover:bg-brand-50 hover:text-brand-700 grid size-9 shrink-0 place-items-center rounded-xl text-stone-600 ring-1 ring-stone-200 transition-colors ring-inset disabled:opacity-40"
          >
            <ChevronRight className="size-4" />
          </button>
        </div>

        <DayPicker
          mode="single"
          locale={ptBR}
          month={visibleMonth}
          onMonthChange={setVisibleMonth}
          selected={selectedDate}
          onSelect={(date) => date && chooseDate(date)}
          disabled={allowFuture ? undefined : { after: today }}
          startMonth={new Date(OLDEST_SELECTABLE_YEAR, 0)}
          endMonth={new Date(lastSelectableYear, 11)}
          hideNavigation
          components={{ MonthCaption: () => <></> }}
          showOutsideDays
          fixedWeeks
        />

        <div className="mt-2 flex items-center justify-between border-t border-stone-100 pt-2">
          <Button type="button" variant="ghost" size="sm" onClick={clear} disabled={value === ''}>
            Limpar
          </Button>
          {isSelectable(today) && (
            <Button type="button" variant="ghost" size="sm" onClick={() => chooseDate(today)}>
              Hoje
            </Button>
          )}
        </div>
      </PopoverContent>
    </Popover>
  )
}
