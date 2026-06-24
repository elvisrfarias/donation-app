import type { ButtonHTMLAttributes, ReactNode, Ref } from 'react'
import { Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'

type Variant = 'primary' | 'brand' | 'secondary' | 'ghost' | 'danger' | 'dark'
type Size = 'sm' | 'md' | 'lg'

const variantClasses: Record<Variant, string> = {
  primary:
    'bg-linear-to-r from-brand-500 to-brand-700 text-white shadow-brand hover:from-brand-600 hover:to-brand-800 hover:shadow-lift active:brightness-95',
  brand:
    'bg-linear-to-r from-brand-500 to-brand-700 text-white shadow-brand hover:from-brand-600 hover:to-brand-800 hover:shadow-lift active:brightness-95',
  secondary:
    'bg-white text-stone-700 shadow-xs ring-1 ring-inset ring-stone-200 hover:bg-brand-50/60 hover:text-brand-700 hover:ring-brand-200',
  ghost: 'text-stone-600 hover:bg-brand-50 hover:text-brand-700',
  danger: 'bg-red-600 text-white shadow-sm hover:bg-red-700 active:bg-red-800',
  dark: 'bg-ink-900 text-white shadow-sm hover:bg-ink-800 active:bg-ink-950',
}

const sizeClasses: Record<Size, string> = {
  sm: 'h-9 gap-1.5 px-4 text-sm',
  md: 'h-11 gap-2 px-5 text-sm',
  lg: 'h-12 gap-2 px-7 text-[0.95rem]',
}

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
  loading?: boolean
  leftIcon?: ReactNode
  ref?: Ref<HTMLButtonElement>
}

export function Button({
  variant = 'primary',
  size = 'md',
  loading = false,
  leftIcon,
  className,
  children,
  disabled,
  ref,
  ...props
}: ButtonProps) {
  return (
    <button
      ref={ref}
      disabled={disabled || loading}
      className={cn(
        'inline-flex items-center justify-center rounded-full font-semibold transition-all duration-150 disabled:cursor-not-allowed disabled:opacity-50',
        variantClasses[variant],
        sizeClasses[size],
        className,
      )}
      {...props}
    >
      {loading ? <Loader2 className="size-4 animate-spin" aria-hidden /> : leftIcon}
      {children}
    </button>
  )
}
