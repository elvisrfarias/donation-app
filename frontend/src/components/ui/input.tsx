import type { ComponentProps } from 'react'
import { cn } from '@/lib/utils'

function Input({ className, type, ...props }: ComponentProps<'input'>) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(
        'flex h-11 w-full min-w-0 rounded-xl border border-input bg-white px-3.5 py-1 text-sm text-stone-900 shadow-xs transition-[color,box-shadow] outline-none',
        'placeholder:text-stone-400 selection:bg-brand-500/20 selection:text-brand-900',
        'file:inline-flex file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-stone-700',
        'hover:border-stone-300 focus-visible:border-brand-500 focus-visible:ring-4 focus-visible:ring-brand-500/10',
        'disabled:cursor-not-allowed disabled:bg-stone-50 disabled:text-stone-400',
        'aria-invalid:border-red-300 aria-invalid:focus-visible:border-red-500 aria-invalid:focus-visible:ring-red-500/10',
        className,
      )}
      {...props}
    />
  )
}

export { Input }
