import { RotateCcw } from 'lucide-react'
import { isRouteErrorResponse, useRouteError } from 'react-router-dom'
import { Button } from '@/views/components/ui/Button'

export function RouteError() {
  const error = useRouteError()

  const message = isRouteErrorResponse(error)
    ? `${error.status} ${error.statusText}`
    : error instanceof Error
      ? error.message
      : 'Erro inesperado.'

  return (
    <div className="grid min-h-dvh place-items-center bg-stone-50 px-6">
      <div className="w-full max-w-md rounded-2xl bg-white p-8 text-center shadow-card ring-1 ring-stone-100">
        <h1 className="text-lg font-bold text-stone-900">Algo deu errado</h1>
        <p className="mt-2 text-sm text-stone-500">
          Não foi possível carregar esta página. Isso costuma acontecer logo após uma
          atualização do sistema — recarregar resolve.
        </p>
        <p className="mt-3 break-words rounded-lg bg-stone-100 px-3 py-2 text-xs text-stone-500">
          {message}
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
