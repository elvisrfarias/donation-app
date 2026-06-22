import type {
  InputHTMLAttributes,
  ReactNode,
  Ref,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from 'react'
import { cn } from '@/lib/utils'

const base =
  'w-full rounded-lg border bg-white px-3 text-sm text-slate-900 transition-colors placeholder:text-slate-400 disabled:bg-slate-50 disabled:text-slate-500'

function stateRing(invalid?: boolean): string {
  return invalid
    ? 'border-red-400 focus:border-red-500 focus:ring-red-500/30'
    : 'border-slate-300 focus:border-brand-500 focus:ring-brand-500/30'
}

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  invalid?: boolean
  ref?: Ref<HTMLInputElement>
}
export function Input({ className, invalid, ref, ...props }: InputProps) {
  return <input ref={ref} className={cn(base, 'h-10', stateRing(invalid), className)} {...props} />
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  invalid?: boolean
  ref?: Ref<HTMLSelectElement>
}
export function Select({ className, invalid, children, ref, ...props }: SelectProps) {
  return (
    <select ref={ref} className={cn(base, 'h-10 pr-8', stateRing(invalid), className)} {...props}>
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
      className={cn(base, 'min-h-20 resize-y py-2', stateRing(invalid), className)}
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
      <label htmlFor={htmlFor} className="text-sm font-medium text-slate-700">
        {label}
        {required && <span className="text-red-500"> *</span>}
      </label>
      {children}
      {hint && !error && <p className="text-xs text-slate-500">{hint}</p>}
      {error && <p className="text-xs font-medium text-red-600">{error}</p>}
    </div>
  )
}
