import { useState, type KeyboardEvent, type ReactNode } from 'react'
import { UserRoundPlus, X } from 'lucide-react'
import { maskCpf } from '@/lib/cpfInput'
import { kinshipTypeLabels, type KinshipType } from '@/models/types'
import { Alert, Badge, Button, FormField, Input, Select } from '@/views/components/ui'

const KINSHIP_TYPES = Object.keys(kinshipTypeLabels) as KinshipType[]
const MIN_CPF_DIGITS = 11

export interface KinshipPanelItem {
  key: string
  label: ReactNode
  onRemove?: () => void
}

interface KinshipsPanelProps {
  items: KinshipPanelItem[]
  isAdding: boolean
  errorMessage?: string | null
  onAdd: (cpf: string, type: KinshipType) => Promise<boolean>
}

export function KinshipsPanel({ items, isAdding, errorMessage, onAdd }: KinshipsPanelProps) {
  const [cpf, setCpf] = useState('')
  const [type, setType] = useState<KinshipType>('Conjuge')

  async function handleAdd() {
    const wasAdded = await onAdd(cpf, type)
    if (wasAdded) setCpf('')
  }

  const cpfDigits = cpf.replace(/\D/g, '')
  const canAdd = !isAdding && cpfDigits.length >= MIN_CPF_DIGITS

  function handleCpfKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== 'Enter') return
    event.preventDefault()
    if (canAdd) void handleAdd()
  }

  return (
    <div className="space-y-3 rounded-xl border border-stone-200 p-4">
      <h3 className="text-sm font-semibold text-stone-700">Vínculos de parentesco</h3>

      {items.length === 0 ? (
        <p className="text-sm text-stone-500">Nenhum vínculo registrado ainda.</p>
      ) : (
        <ul className="flex flex-wrap gap-2">
          {items.map((item) => (
            <li key={item.key}>
              <Badge tone="slate">
                {item.label}
                {item.onRemove && (
                  <button
                    type="button"
                    aria-label="Remover vínculo"
                    onClick={item.onRemove}
                    className="ml-1.5 rounded-full text-stone-500 hover:text-stone-900"
                  >
                    <X className="size-3" />
                  </button>
                )}
              </Badge>
            </li>
          ))}
        </ul>
      )}

      {errorMessage && <Alert tone="error">{errorMessage}</Alert>}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_1fr_auto] sm:items-end">
        <FormField label="CPF do parente" htmlFor="kinship-cpf" required>
          <Input
            id="kinship-cpf"
            inputMode="numeric"
            placeholder="000.000.000-00"
            value={cpf}
            onChange={(event) => setCpf(maskCpf(event.target.value))}
            onKeyDown={handleCpfKeyDown}
          />
        </FormField>
        <FormField label="Parentesco" htmlFor="kinship-type" required>
          <Select
            id="kinship-type"
            value={type}
            onValueChange={(value) => setType(value as KinshipType)}
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
          disabled={!canAdd}
        >
          {isAdding ? 'Adicionando…' : 'Adicionar vínculo'}
        </Button>
      </div>
    </div>
  )
}
