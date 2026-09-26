import type { InputHTMLAttributes, ReactNode, Ref, TextareaHTMLAttributes } from 'react'
import { cn } from '@/lib/utils'
import { inputBase, stateRing } from './fieldStyles'
import { RequiredMark } from './RequiredMark'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  invalid?: boolean
  ref?: Ref<HTMLInputElement>
}
export function Input({ className, invalid, ref, ...props }: InputProps) {
  return (
    <input ref={ref} className={cn(inputBase, 'h-11', stateRing(invalid), className)} {...props} />
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
      className={cn(inputBase, 'min-h-24 resize-y py-2.5', stateRing(invalid), className)}
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
        {required && <RequiredMark />}
      </label>
      {children}
      {hint && !error && <p className="text-xs text-stone-500">{hint}</p>}
      {error && <p className="text-xs font-medium text-red-600">{error}</p>}
    </div>
  )
}
