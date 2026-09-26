import { useState } from 'react'
import {
  kinshipTypeLabels,
  type AddBeneficiaryKinshipInput,
  type KinshipType,
} from '@/models/types'
import { KinshipsPanel } from './KinshipsPanel'

const DUPLICATED_CPF_MESSAGE = 'Este CPF já está na lista de vínculos.'

interface Props {
  items: AddBeneficiaryKinshipInput[]
  onChange: (items: AddBeneficiaryKinshipInput[]) => void
}

function onlyDigits(text: string): string {
  return text.replace(/\D/g, '')
}

/** Vínculos de um beneficiário ainda não salvo: ficam na lista e são gravados junto com o cadastro. */
export function PendingKinshipsEditor({ items, onChange }: Props) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  async function handleAdd(cpf: string, type: KinshipType) {
    const isDuplicated = items.some(
      (item) => onlyDigits(item.relatedBeneficiaryCpf) === onlyDigits(cpf),
    )
    if (isDuplicated) {
      setErrorMessage(DUPLICATED_CPF_MESSAGE)
      return false
    }
    setErrorMessage(null)
    onChange([...items, { relatedBeneficiaryCpf: cpf, type }])
    return true
  }

  return (
    <KinshipsPanel
      items={items.map((item, index) => ({
        key: item.relatedBeneficiaryCpf,
        label: `${kinshipTypeLabels[item.type]}: ${item.relatedBeneficiaryCpf}`,
        onRemove: () => onChange(items.filter((_, itemIndex) => itemIndex !== index)),
      }))}
      isAdding={false}
      errorMessage={errorMessage}
      onAdd={handleAdd}
    />
  )
}
