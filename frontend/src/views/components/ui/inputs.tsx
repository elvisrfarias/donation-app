import type {
  InputHTMLAttributes,
  ReactNode,
  Ref,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from 'react'
import { cn } from '@/lib/utils'

const base =
  'w-full rounded-xl border bg-white px-3.5 text-sm text-stone-900 transition-all duration-150 placeholder:text-stone-400 focus:outline-none disabled:bg-stone-50 disabled:text-stone-400'

function stateRing(invalid?: boolean): string {
  return invalid
    ? 'border-red-300 focus:border-red-500 focus:ring-4 focus:ring-red-500/10'
    : 'border-stone-200 hover:border-stone-300 focus:border-brand-500 focus:ring-4 focus:ring-brand-500/10'
}

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  invalid?: boolean
  ref?: Ref<HTMLInputElement>
}
export function Input({ className, invalid, ref, ...props }: InputProps) {
  return <input ref={ref} className={cn(base, 'h-11', stateRing(invalid), className)} {...props} />
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  invalid?: boolean
  ref?: Ref<HTMLSelectElement>
}
export function Select({ className, invalid, children, ref, ...props }: SelectProps) {
  return (
    <select ref={ref} className={cn(base, 'h-11 pr-9', stateRing(invalid), className)} {...props}>
      {children}
    </select>
  )
}

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  invalid?: boolean
  ref?: Ref<HTMLTextAreaElement>
}
export function Textarea({ className, invalid, ref, ...props }: TextareaProps) {
  return (
    <textarea
      ref={ref}
      className={cn(base, 'min-h-24 resize-y py-2.5', stateRing(invalid), className)}
      {...props}
    />
  )
}

interface FormFieldProps {
  label: string
  htmlFor?: string
  error?: string
  hint?: string
  required?: boolean
  className?: string
  children: ReactNode
}
export function FormField({
  label,
  htmlFor,
  error,
  hint,
  required,
  className,
  children,
}: FormFieldProps) {
  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      <label htmlFor={htmlFor} className="text-sm font-semibold text-stone-700">
        {label}
        {required && <span className="text-brand-600"> *</span>}
      </label>
      {children}
      {hint && !error && <p className="text-xs text-stone-500">{hint}</p>}
      {error && <p className="text-xs font-medium text-red-600">{error}</p>}
    </div>
  )
}
