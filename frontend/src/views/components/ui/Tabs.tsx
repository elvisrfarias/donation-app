import { useRef, type KeyboardEvent, type ReactNode } from 'react'
import { cn } from '@/lib/utils'

export interface TabItem {
  value: string
  label: string
  icon?: ReactNode
  badge?: number
}

interface TabsProps {
  tabs: TabItem[]
  value: string
  onValueChange: (value: string) => void
  ariaLabel: string
  className?: string
}

function tabId(value: string): string {
  return `tab-${value}`
}

function tabPanelId(value: string): string {
  return `tabpanel-${value}`
}

export function Tabs({ tabs, value, onValueChange, ariaLabel, className }: TabsProps) {
  const tabRefs = useRef<Record<string, HTMLButtonElement | null>>({})

  function handleKeyDown(event: KeyboardEvent, index: number) {
    const step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0
    if (step === 0) return
    event.preventDefault()
    const next = tabs[(index + step + tabs.length) % tabs.length]
    onValueChange(next.value)
    tabRefs.current[next.value]?.focus()
  }

  return (
    <div
      role="tablist"
      aria-label={ariaLabel}
      className={cn('flex gap-1 border-b border-stone-200', className)}
    >
      {tabs.map((tab, index) => {
        const active = tab.value === value
        return (
          <button
            key={tab.value}
            ref={(element) => {
              tabRefs.current[tab.value] = element
            }}
            id={tabId(tab.value)}
            type="button"
            role="tab"
            aria-selected={active}
            aria-controls={tabPanelId(tab.value)}
            tabIndex={active ? 0 : -1}
            onClick={() => onValueChange(tab.value)}
            onKeyDown={(event) => handleKeyDown(event, index)}
            className={cn(
              '-mb-px inline-flex items-center gap-2 border-b-2 px-4 py-2.5 text-sm font-semibold transition-colors',
              active
                ? 'border-brand-600 text-brand-700'
                : 'border-transparent text-stone-500 hover:border-stone-300 hover:text-stone-800',
            )}
          >
            {tab.icon}
            {tab.label}
            {tab.badge !== undefined && tab.badge > 0 && (
              <span className="grid min-w-5 place-items-center rounded-full bg-amber-100 px-1.5 text-[11px] font-bold text-amber-700">
                {tab.badge}
              </span>
            )}
          </button>
        )
      })}
    </div>
  )
}

interface TabPanelProps {
  value: string
  activeValue: string
  children: ReactNode
  className?: string
}

/** O painel inativo fica montado (só oculto) para não perder rascunhos não salvos ao trocar de aba. */
export function TabPanel({ value, activeValue, children, className }: TabPanelProps) {
  return (
    <div
      role="tabpanel"
      id={tabPanelId(value)}
      aria-labelledby={tabId(value)}
      hidden={value !== activeValue}
      className={className}
    >
      {children}
    </div>
  )
}
