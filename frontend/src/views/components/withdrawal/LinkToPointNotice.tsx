import { Link2 } from 'lucide-react'
import { Alert, Button } from '@/views/components/ui'

interface LinkToPointNoticeProps {
  pointName: string
  isLinking: boolean
  errorMessage?: string
  onLink: () => void
}

/**
 * Aviso do balcão quando a pessoa encontrada pelo CPF ainda não é atendida pelo ponto: os dados
 * completos e o histórico só aparecem depois do vínculo, que é a decisão do gestor.
 */
export function LinkToPointNotice({
  pointName,
  isLinking,
  errorMessage,
  onLink,
}: LinkToPointNoticeProps) {
  return (
    <Alert tone="info" title={`Ainda não atendida por ${pointName}`}>
      <p>
        Esta pessoa está cadastrada no município, mas não está vinculada a este ponto. Vincule-a
        para ver o histórico de retiradas e registrar a retirada.
      </p>
      {errorMessage && <p className="mt-1 font-medium text-red-700">{errorMessage}</p>}
      <Button
        className="mt-3"
        size="sm"
        variant="brand"
        leftIcon={<Link2 className="size-4" />}
        loading={isLinking}
        onClick={onLink}
      >
        Vincular a {pointName}
      </Button>
    </Alert>
  )
}
