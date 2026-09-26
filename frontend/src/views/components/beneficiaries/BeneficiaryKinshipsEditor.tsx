import { kinshipTypeLabels, type Beneficiary, type KinshipType } from '@/models/types'
import { useAddBeneficiaryKinship } from '@/viewmodels'
import { KinshipsPanel } from './KinshipsPanel'

interface Props {
  beneficiary: Beneficiary
}

/** Vínculos de parentesco de um beneficiário já salvo: cada vínculo adicionado é gravado na hora. */
export function BeneficiaryKinshipsEditor({ beneficiary }: Props) {
  const { addKinship, isPending, error, reset } = useAddBeneficiaryKinship()

  async function handleAdd(cpf: string, type: KinshipType) {
    reset()
    try {
      await addKinship({
        beneficiaryId: beneficiary.id,
        input: { relatedBeneficiaryCpf: cpf, type },
      })
      return true
    } catch {
      return false
    }
  }

  return (
    <KinshipsPanel
      items={beneficiary.kinships.map((kinship) => ({
        key: kinship.relatedBeneficiaryId,
        label: `${kinshipTypeLabels[kinship.type]}: ${kinship.relatedBeneficiaryName} (${kinship.relatedBeneficiaryCpf})`,
      }))}
      isAdding={isPending}
      errorMessage={error?.displayMessage}
      onAdd={handleAdd}
    />
  )
}
