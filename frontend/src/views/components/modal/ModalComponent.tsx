import type { ReactNode } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { cn } from '@/lib/utils'

/** Larguras máximas — `lg` é o padrão e comporta bem um formulário de 2 colunas. */
const sizeClasses = {
  sm: 'max-w-sm',
  md: 'max-w-md',
  lg: 'max-w-lg',
  xl: 'max-w-2xl',
} as const

export interface ModalComponentProps {
  /** Estado controlado de abertura. */
  open?: boolean
  onOpenChange?: (open: boolean) => void
  /** Disparador opcional (renderizado via `asChild`); dispensa o controle externo. */
  trigger?: ReactNode
  title?: string
  description?: string
  children: ReactNode
  /** Rodapé opcional (ex.: ações de confirmar/cancelar). Forms costumam trazer o próprio submit. */
  footer?: ReactNode
  size?: keyof typeof sizeClasses
  showCloseButton?: boolean
  /** Classe extra aplicada ao container de conteúdo. */
  className?: string
}

/**
 * Modal reutilizável sobre o Dialog do shadcn. Cuida de overlay, título,
 * descrição, botão de fechar e rodapé — basta passar o conteúdo em `children`.
 * Projetado para futuramente hospedar formulários: renderize um `<FormComponent />`
 * como filho e ele traz o próprio submit; o modal só provê o invólucro.
 */
export function ModalComponent({
  open,
  onOpenChange,
  trigger,
  title,
  description,
  children,
  footer,
  size = 'lg',
  showCloseButton = true,
  className,
}: ModalComponentProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      {trigger && <DialogTrigger asChild>{trigger}</DialogTrigger>}
      <DialogContent showCloseButton={showCloseButton} className={cn(sizeClasses[size], className)}>
        {(title || description) && (
          <DialogHeader>
            {title && <DialogTitle>{title}</DialogTitle>}
            {description && <DialogDescription>{description}</DialogDescription>}
          </DialogHeader>
        )}
        {/* Miolo rolável: o cabeçalho/rodapé ficam fixos e só o conteúdo rola
            quando passa da moldura. `-mx-6 px-6` mantém o respiro lateral e
            evita que a barra de rolagem fique colada no conteúdo. */}
        <div className="-mx-6 min-h-0 flex-1 overflow-y-auto px-6">{children}</div>
        {footer && <DialogFooter>{footer}</DialogFooter>}
      </DialogContent>
    </Dialog>
  )
}
