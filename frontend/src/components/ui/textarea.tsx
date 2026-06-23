import type { ComponentProps } from 'react'
import { cn } from '@/lib/utils'

function Textarea({ className, ...props }: ComponentProps<'textarea'>) {
  return (
    <textarea
      data-slot="textarea"
      className={cn(
        'flex min-h-24 w-full rounded-xl border border-input bg-white px-3.5 py-2.5 text-sm text-stone-900 shadow-xs transition-[color,box-shadow] outline-none',
        'resize-y placeholder:text-stone-400',
        'hover:border-stone-300 focus-visible:border-brand-500 focus-visible:ring-4 focus-visible:ring-brand-500/10',
        'disabled:cursor-not-allowed disabled:bg-stone-50 disabled:text-stone-400',
        'aria-invalid:border-red-300 aria-invalid:focus-visible:border-red-500 aria-invalid:focus-visible:ring-red-500/10',
        className,
      )}
      {...props}
    />
  )
}

export { Textarea }
