import { Component, type ErrorInfo, type ReactNode } from 'react'
import { RotateCcw } from 'lucide-react'
import { logger } from '@/lib/logger'
import { Button } from '@/views/components/ui'

interface ErrorBoundaryProps {
  children: ReactNode
}

interface ErrorBoundaryState {
  error: Error | null
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    logger.error('Erro não tratado capturado pelo ErrorBoundary', {
      message: error.message,
      stack: error.stack,
      componentStack: errorInfo.componentStack,
    })
  }

  render() {
    if (!this.state.error) {
      return this.props.children
    }

    return (
      <div className="grid min-h-dvh place-items-center bg-stone-50 px-6">
        <div className="w-full max-w-md rounded-2xl bg-white p-8 text-center shadow-card ring-1 ring-stone-100">
          <h1 className="text-lg font-bold text-stone-900">Algo deu errado</h1>
          <p className="mt-2 text-sm text-stone-500">
            Um erro inesperado interrompeu esta tela. Recarregar costuma resolver — se persistir,
            avise o suporte.
          </p>
          <p className="mt-3 break-words rounded-lg bg-stone-100 px-3 py-2 text-xs text-stone-500">
            {this.state.error.message}
          </p>
          <Button
            variant="brand"
            className="mt-5 w-full"
            leftIcon={<RotateCcw className="size-4" />}
            onClick={() => window.location.reload()}
          >
            Recarregar
          </Button>
        </div>
      </div>
    )
  }
}
