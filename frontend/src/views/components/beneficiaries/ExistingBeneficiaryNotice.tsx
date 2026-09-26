import { Link2, Pencil } from 'lucide-react'
import type { Beneficiary } from '@/models/types'
import { Alert, Button } from '@/views/components/ui'

interface ExistingBeneficiaryNoticeProps {
  beneficiary: Beneficiary
  pointName: string
  alreadyLinked: boolean
  isLinking: boolean
  errorMessage?: string
  onLink: () => void
  onEdit: () => void
}

export function ExistingBeneficiaryNotice({
  beneficiary,
  pointName,
  alreadyLinked,
  isLinking,
  errorMessage,
  onLink,
  onEdit,
}: ExistingBeneficiaryNoticeProps) {
  return (
    <Alert tone="warning" title="Esta pessoa já está cadastrada">
      <p>
        <strong>{beneficiary.name}</strong> (CPF {beneficiary.cpf}){' '}
        {alreadyLinked
          ? `já é atendida por ${pointName}.`
          : `é atendida por outro ponto. Se ${pointName} também a atende, vincule-a para vê-la na sua lista e poder atualizar os dados.`}
      </p>
      {errorMessage && <p className="mt-1 font-medium text-red-700">{errorMessage}</p>}
      <div className="mt-3 flex flex-wrap gap-2">
        {alreadyLinked ? (
          <Button
            size="sm"
            variant="secondary"
            leftIcon={<Pencil className="size-4" />}
            onClick={onEdit}
          >
            Editar dados
          </Button>
        ) : (
          <Button
            size="sm"
            variant="brand"
            leftIcon={<Link2 className="size-4" />}
            loading={isLinking}
            onClick={onLink}
          >
            Vincular a {pointName}
          </Button>
        )}
      </div>
    </Alert>
  )
}
