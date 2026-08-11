import { useState } from 'react'
import { UserRoundPlus } from 'lucide-react'
import { kinshipTypeLabels, type Beneficiary, type KinshipType } from '@/models/types'
import { useAddBeneficiaryKinship } from '@/viewmodels'
import { Alert, Badge, Button, FormField, Input, Select } from '@/views/components/ui'

const KINSHIP_TYPES = Object.keys(kinshipTypeLabels) as KinshipType[]
const MIN_CPF_DIGITS = 11

interface Props {
  beneficiary: Beneficiary
}

/**
 * Editor de vínculos de parentesco de um beneficiário — usado no cadastro/edição.
 * Só aparece com um beneficiário já salvo (precisa de um Id pra vincular).
 */
export function BeneficiaryKinshipsEditor({ beneficiary }: Props) {
  const { addKinship, isPending, error, reset } = useAddBeneficiaryKinship()
  const [cpf, setCpf] = useState('')
  const [type, setType] = useState<KinshipType>('Conjuge')

  async function handleAdd() {
    reset()
    await addKinship({
      familyId: beneficiary.familyId,
      beneficiaryId: beneficiary.id,
      input: { relatedBeneficiaryCpf: cpf, type },
    })
    setCpf('')
  }

  const cpfDigits = cpf.replace(/\D/g, '')

  return (
    <div className="space-y-3 rounded-xl border border-stone-200 p-4">
      <h3 className="text-sm font-semibold text-stone-700">Vínculos de parentesco</h3>

      {beneficiary.kinships.length === 0 ? (
        <p className="text-sm text-stone-500">Nenhum vínculo registrado ainda.</p>
      ) : (
        <ul className="flex flex-wrap gap-2">
          {beneficiary.kinships.map((kinship) => (
            <li key={kinship.relatedBeneficiaryId}>
              <Badge tone="slate">
                {kinshipTypeLabels[kinship.type]}: {kinship.relatedBeneficiaryName} (
                {kinship.relatedBeneficiaryCpf})
              </Badge>
            </li>
          ))}
        </ul>
      )}

      {error && <Alert tone="error">{error.displayMessage}</Alert>}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
        <FormField label="CPF do parente" htmlFor="kinship-cpf">
          <Input
            id="kinship-cpf"
            placeholder="000.000.000-00"
            value={cpf}
            onChange={(e) => setCpf(e.target.value)}
          />
        </FormField>
        <FormField label="Parentesco" htmlFor="kinship-type">
          <Select
            id="kinship-type"
            value={type}
            onChange={(e) => setType(e.target.value as KinshipType)}
          >
            {KINSHIP_TYPES.map((value) => (
              <option key={value} value={value}>
                {kinshipTypeLabels[value]}
              </option>
            ))}
          </Select>
        </FormField>
        <Button
          type="button"
          variant="secondary"
          leftIcon={<UserRoundPlus className="size-4" />}
          onClick={handleAdd}
          disabled={isPending || cpfDigits.length < MIN_CPF_DIGITS}
        >
          {isPending ? 'Adicionando…' : 'Adicionar vínculo'}
        </Button>
      </div>
    </div>
  )
}
